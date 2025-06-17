using System;

namespace Shared.Models
{
    public class OutboxEvent
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public bool IsProcessed { get; set; }
    }
} 