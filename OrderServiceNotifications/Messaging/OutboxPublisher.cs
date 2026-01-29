using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderServiceNotifications.Data;
using OrderServiceNotifications.Model;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace OrderServiceNotifications.Messaging
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisher> _logger;

        public OutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisher starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    // ensure database is reachable before querying outbox
                    var canConnect = false;
                    try
                    {
                        canConnect = await db.Database.CanConnectAsync(stoppingToken);
                    }
                    catch (Exception canEx)
                    {
                        _logger.LogWarning(canEx, "Database connectivity check failed — will retry shortly.");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    if (!canConnect)
                    {
                        _logger.LogWarning("Database not ready for queries — will retry shortly.");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    // Ensure the schema/tables exist (defensive - sometimes DB created but tables missing)
                    try
                    {
                        await db.Database.EnsureCreatedAsync(stoppingToken);
                    }
                    catch (Exception ensureEx)
                    {
                        _logger.LogWarning(ensureEx, "EnsureCreatedAsync failed; continuing and will retry queries.");
                    }

                    List<OutboxEvent>? events = null;
                    try
                    {
                        events = await db.OutboxEvents
                            .Where(e => !e.Published)
                            .OrderBy(e => e.OccurredAt)
                            .Take(10)
                            .ToListAsync(stoppingToken);
                    }
                    catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(sqlEx, "OutboxEvents table not found; attempting to create schema and retry.");
                        try
                        {
                            await db.Database.EnsureCreatedAsync(stoppingToken);
                        }
                        catch (Exception ensureEx)
                        {
                            _logger.LogError(ensureEx, "Failed to create database schema after detecting missing OutboxEvents table.");
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                            continue;
                        }

                        // retry the query once
                        try
                        {
                            events = await db.OutboxEvents
                                .Where(e => !e.Published)
                                .OrderBy(e => e.OccurredAt)
                                .Take(10)
                                .ToListAsync(stoppingToken);
                        }
                        catch (Exception retryEx)
                        {
                            _logger.LogError(retryEx, "Retry after EnsureCreated failed when querying OutboxEvents.");
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                            continue;
                        }
                    }

                    if (events == null)
                    {
                        // unexpected null, wait and retry
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    if (!events.Any())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                   

                    var factory = new ConnectionFactory
                    {
                        HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                        UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
                        Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest"
                    };
                    // use synchronous model to access IModel API
                    using var connection = await factory.CreateConnectionAsync();
                    using var channel = await connection.CreateChannelAsync();

                    // ensure queue exists (async API)
                    await channel.QueueDeclareAsync(queue: "order_created",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    foreach (var ev in events)
                    {
                        try
                        {
                            // try to extract order and customer info from payload for better logs
                            string? orderIdStr = null;
                            string? customerEmail = null;
                            try
                            {
                                using var doc = JsonDocument.Parse(ev.Payload);
                                var root = doc.RootElement;
                                if (root.TryGetProperty("orderId", out var orderIdEl)) orderIdStr = orderIdEl.GetGuid().ToString();
                                if (root.TryGetProperty("customerEmail", out var emailEl)) customerEmail = emailEl.GetString();
                            }
                            catch { /* ignore - payload may not be JSON */ }

                            var body = Encoding.UTF8.GetBytes(ev.Payload);
                            // publish message (async API)
                            _logger.LogInformation("Publishing event {EventId} (type={EventType}) for Order {OrderId} to customer {CustomerEmail}", ev.EventId, ev.EventType, orderIdStr ?? "<unknown>", customerEmail ?? "<unknown>");
                            await channel.BasicPublishAsync(exchange: string.Empty,
                                                 routingKey: "order_created",
                                                 body: body);

                            ev.Published = true;
                            ev.PublishedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Published outbox event {EventId}", ev.EventId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to publish outbox event {EventId}", ev.EventId);
                            // leave unpublished for retry
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutboxPublisher encountered an error");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("OutboxPublisher stopping");
        }
    }
}
