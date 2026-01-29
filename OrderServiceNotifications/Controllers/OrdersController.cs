using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderServiceNotifications.Data;
using OrderServiceNotifications.Model;
using OrderServiceNotifications.Contracts;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace OrderServiceNotifications.Controllers
{


    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _db;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderDbContext db, ILogger<OrdersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Action"] = "CreateOrder",
                ["CustomerEmail"] = request.CustomerEmail
            });

            var orderId = Guid.NewGuid();

            var order = new Order
            {
                Id = orderId,
                CustomerEmail = request.CustomerEmail,
                ProductCode = request.ProductCode,
                Quantity = request.Quantity
            };

            var @event = new
            {
                eventId = Guid.NewGuid(),
                occurredAt = DateTime.UtcNow,
                orderId = order.Id,
                customerEmail = order.CustomerEmail,
                productCode = order.ProductCode,
                quantity = order.Quantity
            };

            var payload = JsonSerializer.Serialize(@event);

            var outbox = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventId = Guid.Parse(@event.eventId.ToString()),
                OccurredAt = DateTime.UtcNow,
                EventType = "ORDER_CREATED",
                Payload = payload,
                Published = false
            };

            try
            {
                _db.Orders.Add(order);
                _db.OutboxEvents.Add(outbox);
                await _db.SaveChangesAsync();

                // Log a friendly, human-oriented statement summarizing the action
                _logger.LogInformation("Created order {OrderId} for customer {CustomerEmail}: {Quantity}x {ProductCode}. Outbox event {EventId} queued for publishing.",
                    order.Id, order.CustomerEmail, order.Quantity, order.ProductCode, outbox.EventId);

                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for {CustomerEmail}", request.CustomerEmail);
                return StatusCode(500, "Failed to create order");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.Orders.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }

    // CreateOrderRequest moved to Contracts/CreateOrderRequest.cs
}