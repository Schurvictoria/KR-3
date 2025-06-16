namespace Shared.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
        public string Status { get; set; } = "Created";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class OrderStatus
    {
        public const string Created = "Created";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
} 