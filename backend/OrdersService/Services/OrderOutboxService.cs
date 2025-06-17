using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using RabbitMQ.Client;
using Shared.Models;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrdersService.Services
{
    public class OrderOutboxService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string QueueName = "order-payments";
        private readonly OrdersDbContext _db;

        public OrderOutboxService(IServiceProvider serviceProvider, OrdersDbContext db)
        {
            _serviceProvider = serviceProvider;
            _db = db;

            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        }

        public void SendOrderForPayment(Shared.Models.Order order)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var outboxEvent = new Shared.Models.OutboxEvent {
                Id = Guid.NewGuid(),
                Type = "OrderCreated",
                Payload = JsonSerializer.Serialize(new {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    Amount = order.Amount
                }),
                OccurredAt = DateTime.UtcNow,
                IsProcessed = false
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
                    .Where(e => !e.IsProcessed)
                    .ToListAsync(stoppingToken);
                foreach (var evt in pendingEvents)
                {
                    var body = Encoding.UTF8.GetBytes(evt.Payload);
                    _channel.BasicPublish("", QueueName, null, body);
                    evt.IsProcessed = true;
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

        public async Task MarkEventProcessedAsync(Guid eventId)
        {
            var evt = await _db.Outbox.FindAsync(eventId);
            if (evt != null)
            {
                evt.IsProcessed = true;
                await _db.SaveChangesAsync();
            }
        }
    }
}
