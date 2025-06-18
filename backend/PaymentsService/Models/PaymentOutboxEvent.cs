using System;

namespace PaymentsService.Models
{
    public class PaymentOutboxEvent
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public bool IsProcessed { get; set; }
    }
} 