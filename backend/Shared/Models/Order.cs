using System;

namespace Shared.Models
{
    public enum OrderStatus
    {
        Created,
        Processing,
        Completed,
        Cancelled
    }

    public class Order
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 