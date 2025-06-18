using OrdersService.Data;
using Shared.Models;
using RabbitMQ.Client;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrdersService.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrdersDbContext _db;
        private readonly IConnection _rabbit;

        public OrderService(OrdersDbContext db, IConnection rabbit)
        {
            _db = db;
            _rabbit = rabbit;
        }

        public async Task<Guid> CreateOrderAsync(string product, decimal amount)
        {
            var order = new Shared.Models.Order
            {
                Id = Guid.NewGuid(),
                UserId = product,
                Amount = amount,
                Description = string.Empty,
                Status = Shared.Models.OrderStatus.Created
            };
            _db.Orders.Add(order);

            _db.OutboxEvents.Add(new Shared.Models.OutboxEvent
            {
                Id = Guid.NewGuid(),
                Type = "OrderCreated",
                Payload = JsonSerializer.Serialize(order),
                OccurredAt = DateTime.UtcNow,
                IsProcessed = false
            });

            await _db.SaveChangesAsync();
            return order.Id;
        }
    }
}
