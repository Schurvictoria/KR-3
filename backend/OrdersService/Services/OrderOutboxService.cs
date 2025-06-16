using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using RabbitMQ.Client;
using Shared.Models;
using System.Text;
using System.Text.Json;

namespace OrdersService.Services
{
    public class OrderOutboxService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string QueueName = "order-payments";

        public OrderOutboxService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        }

        public void SendOrderForPayment(Order order)
        {
            var message = new
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            _channel.BasicPublish("", QueueName, null, body);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                var pendingOrders = await dbContext.Orders
                    .Where(o => o.Status == OrderStatus.Created)
                    .ToListAsync(stoppingToken);

                foreach (var order in pendingOrders)
                {
                    SendOrderForPayment(order);
                    order.Status = OrderStatus.Processing;
                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
            base.Dispose();
        }
    }
}
