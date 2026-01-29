using System.Text.Json.Serialization;

namespace NotificationService.Models
{
    public class Notification
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string Email { get; set; } = null!;

        public string Type { get; set; } = null!;

        public bool Delivered { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // EventId is used for idempotency
        public Guid EventId { get; set; }
    }
}
