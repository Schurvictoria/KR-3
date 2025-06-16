namespace OrdersService.Models
{
    public class CreateOrderRequest
    {
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
    }
}