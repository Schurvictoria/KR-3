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
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var outboxEvent = new OutboxEvent {
                Payload = JsonSerializer.Serialize(new {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    Amount = order.Amount
                })
            };
            dbContext.Add(outboxEvent);
            dbContext.SaveChanges();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var pendingEvents = await dbContext.OutboxEvents
                    .Where(e => !e.Processed)
                    .ToListAsync(stoppingToken);
                foreach (var evt in pendingEvents)
                {
                    var body = Encoding.UTF8.GetBytes(evt.Payload);
                    _channel.BasicPublish("", QueueName, null, body);
                    evt.Processed = true;
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

    public class OutboxEvent {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = "OrderCreated";
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Processed { get; set; } = false;
    }
}
