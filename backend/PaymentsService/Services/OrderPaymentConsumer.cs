using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PaymentsService.Services
{
    public class OrderPaymentConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "order-payments";

        public OrderPaymentConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var order = JsonSerializer.Deserialize<OrderPaymentRequest>(message);

                if (order == null) return;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                var account = await dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == order.UserId);
                if (account == null)
                {
                    // Handle case when account doesn't exist
                    return;
                }

                if (account.Balance >= order.Amount)
                {
                    account.Balance -= order.Amount;
                    await dbContext.SaveChangesAsync();

                    // Publish payment success message
                    var successMessage = new PaymentResponse
                    {
                        OrderId = order.OrderId,
                        Status = "Success",
                        UserId = order.UserId
                    };

                    var successBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(successMessage));
                    _channel.BasicPublish("", "payment-results", null, successBody);
                }
                else
                {
                    // Publish payment failure message
                    var failureMessage = new PaymentResponse
                    {
                        OrderId = order.OrderId,
                        Status = "Failed",
                        UserId = order.UserId
                    };

                    var failureBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(failureMessage));
                    _channel.BasicPublish("", "payment-results", null, failureBody);
                }
            };

            _channel.BasicConsume(QueueName, true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
            base.Dispose();
        }
    }

    public class OrderPaymentRequest
    {
        public required string OrderId { get; set; }
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentResponse
    {
        public required string OrderId { get; set; }
        public required string Status { get; set; }
        public required string UserId { get; set; }
    }
}
