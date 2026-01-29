using System;
namespace OrderServiceNotifications.Model
{
    public class OutboxEvent
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }

        public DateTime OccurredAt { get; set; }

        public string EventType { get; set; } = null!;

        public string Payload { get; set; } = null!;

        public bool Published { get; set; } = false;

        public DateTime? PublishedAt { get; set; }
    }
}
