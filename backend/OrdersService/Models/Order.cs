using Shared.Models;

namespace OrdersService.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } // Для CAS
    }
}
