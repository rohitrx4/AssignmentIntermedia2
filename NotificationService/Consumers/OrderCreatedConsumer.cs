using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Data;
using NotificationService.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Consumers
{
    public class OrderCreatedConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(IServiceProvider serviceProvider, ILogger<OrderCreatedConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderCreatedConsumer starting");

            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "user",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "password"
            };

            IConnection? connection = null;
            RabbitMQ.Client.IChannel? channel = null;

            // Retry loop to create connection and channel
            var attempt = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    attempt++;
                    _logger.LogInformation("Attempting to connect to RabbitMQ (attempt {Attempt})...", attempt);
                    connection = await factory.CreateConnectionAsync();
                    channel = await connection.CreateChannelAsync();
                    _logger.LogInformation("Connected to RabbitMQ and channel created");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ not ready yet (attempt {Attempt}). Will retry.", attempt);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Min(5 * attempt, 30)), stoppingToken);
                    }
                    catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            if (connection == null || channel == null)
            {
                _logger.LogWarning("RabbitMQ connection/channel was not established before shutdown.");
                return;
            }

            // Ensure queue exists
            await channel.QueueDeclareAsync(queue: "order_created", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    var doc = JsonDocument.Parse(message);
                    var root = doc.RootElement;
                    Guid eventId = root.GetProperty("eventId").GetGuid();
                    Guid orderId = root.GetProperty("orderId").GetGuid();
                    var customerEmail = root.GetProperty("customerEmail").GetString() ?? string.Empty;

                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                    var exists = await db.Notifications.AnyAsync(n => n.EventId == eventId);
                    if (!exists)
                    {
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderId,
                            Email = customerEmail,
                            Type = "ORDER_CREATED",
                            Delivered = true,
                            ErrorMessage = "Email sent",
                            CreatedAt = DateTime.UtcNow,
                            EventId = eventId
                        };
                        // simulate sending email/SMS and log it for observability
                        _logger.LogInformation("Simulating delivery for Order {OrderId} to {Email}", orderId, customerEmail);
                        db.Notifications.Add(notification);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Notification created and delivery recorded for Order {OrderId} Event {EventId}", orderId, eventId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed processing message: {Message}", message);
                }

                // handler completes
            };

            await channel.BasicConsumeAsync(queue: "order_created", autoAck: true, consumer: consumer);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            finally
            {
                try { channel?.Dispose(); } catch { }
                try { connection?.Dispose(); } catch { }
            }
        }
    }
}
