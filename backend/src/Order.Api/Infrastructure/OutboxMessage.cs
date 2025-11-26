using System;
using System.ComponentModel.DataAnnotations;

namespace Order.Api.Infrastructure
{
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Destination { get; set; }
        public string Payload { get; set; }
        public string CorrelationId { get; set; }
        public string EventType { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public bool Dispatched { get; set; } = false;
        public DateTime? DispatchedAt { get; set; }
        public int DispatchAttempts { get; set; } = 0;
    }
}
