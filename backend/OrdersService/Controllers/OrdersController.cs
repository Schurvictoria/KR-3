using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Services;
using Shared.Models;
using Microsoft.Extensions.Configuration;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        private readonly OrderOutboxService _outboxService;
        private readonly IConfiguration _config;

        public OrdersController(OrdersDbContext db, OrderOutboxService outboxService, IConfiguration config)
        {
            _db = db;
            _outboxService = outboxService;
            _config = config;
            StartPaymentResultConsumer();
        }

        private void StartPaymentResultConsumer()
        {
            var factory = new RabbitMQ.Client.ConnectionFactory() { HostName = _config["RabbitMq:Host"] ?? "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "payment_results", durable: false, exclusive: false, autoDelete: false, arguments: null);
            var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                var result = System.Text.Json.JsonSerializer.Deserialize<PaymentResultMessage>(body);
                if (result != null)
                {
                    var order = _db.Orders.FirstOrDefault(o => o.Id == result.OrderId);
                    if (order != null)
                    {
                        order.Status = result.Status == "FINISHED" ? OrderStatus.Completed : OrderStatus.Cancelled;
                        _db.SaveChanges();
                    }
                }
            };
            channel.BasicConsume(
                queue: "payment_results",
                autoAck: true,
                consumerTag: "",
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer);
        }

        private class PaymentResultMessage
        {
            public Guid OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Amount = request.Amount,
                Description = request.Description,
                Status = OrderStatus.Created
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Outbox: отправить задачу на оплату
            _outboxService.SendOrderForPayment(order);

            return Ok(order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrders(string userId)
        {
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders);
        }
    }

    public class CreateOrderRequest
    {
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
    }
}
