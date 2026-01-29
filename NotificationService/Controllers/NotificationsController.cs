using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationDbContext _db;

        public NotificationsController(NotificationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? orderId)
        {
            try
            {
                if (orderId.HasValue)
                    return Ok(await _db.Notifications.Where(n => n.OrderId == orderId.Value).ToListAsync());

                return Ok(await _db.Notifications.ToListAsync());
            }
            catch (Exception ex)
            {
                // Log and return 503 with generic message. The logged exception will contain SQL details for diagnosis.
                // Obtain logger from HttpContext if available
                var logger = HttpContext.RequestServices.GetService(typeof(ILogger<NotificationsController>)) as ILogger;
                logger?.LogError(ex, "Error fetching notifications from database.");
                return StatusCode(503, "Database unavailable or error fetching notifications. Check server logs for details.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();
            return Ok(n);
        }
    }
}
