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

        // Реализация метода из интерфейса
        public async Task<Guid> CreateOrderAsync(string product, decimal amount)
        {
            // Создаём заказ
            var order = new Shared.Models.Order
            {
                Id = Guid.NewGuid(),
                UserId = product,             // здесь product используем как идентификатор пользователя
                Amount = amount,
                Description = string.Empty,   // описание сейчас пустое
                Status = Shared.Models.OrderStatus.Created
            };
            _db.Orders.Add(order);

            // Создаём событие в outbox
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
