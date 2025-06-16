namespace PaymentsService.Models
{
    public class Account
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public decimal Balance { get; set; }
    }

    public class CreateAccountRequest
    {
        public required string UserId { get; set; }
    }

    public class TopUpRequest
    {
        public decimal Amount { get; set; }
    }
}
