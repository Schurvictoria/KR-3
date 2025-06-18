// PaymentsService/Services/OrderPaymentConsumer.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsService.Data;
using PaymentsService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentsService.Services
{
    public class OrderPaymentConsumer : BackgroundService
    {
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "order-payments";

        public OrderPaymentConsumer(IServiceProvider serviceProvider, IConfiguration config)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory
            {
                HostName            = config["RabbitMQ:Host"]     ?? "rabbitmq",
                Port                = ushort.Parse(config["RabbitMQ:Port"] ?? "5672"),
                UserName            = config["RabbitMQ:User"]     ?? "guest",
                Password            = config["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            // простой retry
            IConnection conn = null;
            for (var i = 0; i < 5; i++)
            {
                try { conn = factory.CreateConnection(); break; }
                catch
                {
                    Thread.Sleep(3000);
                    if (i == 4) throw;
                }
            }

            _channel = conn.CreateModel();
            _channel.QueueDeclare(queue: QueueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (s, ea) =>
            {
                var json        = Encoding.UTF8.GetString(ea.Body.ToArray());
                var orderReq    = JsonSerializer.Deserialize<OrderPaymentRequest>(json);
                if (orderReq is null) return;

                using var scope = _serviceProvider.CreateScope();
                var db          = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                // 1) Сохраняем inbox
                var inbox = new PaymentInboxEvent {
                    Id          = Guid.NewGuid(),
                    Type        = "OrderPaymentRequest",
                    Payload     = json,
                    OccurredAt  = DateTime.UtcNow,
                    IsProcessed = false
                };
                db.PaymentInboxEvents.Add(inbox);
                await db.SaveChangesAsync(stoppingToken);

                // 2) Бизнес-логика
                var account = await db.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == orderReq.UserId, stoppingToken);
                
                var response = new PaymentResponse {
                    OrderId = orderReq.OrderId,
                    UserId  = orderReq.UserId,
                    Status  = (account != null && account.Balance >= orderReq.Amount)
                                ? "Success"
                                : "Failed"
                };

                if (response.Status == "Success")
                {
                    var originalRV = account.RowVersion;
                    account.Balance -= orderReq.Amount;
                    db.Entry(account).OriginalValues["RowVersion"] = originalRV;
                    try
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // при конфликте просто не проводим платёж
                        response.Status = "Failed";
                    }
                }

                // 3) Отмечаем inbox как обработанный
                inbox.IsProcessed = true;
                await db.SaveChangesAsync(stoppingToken);

                // 4) Создаём outbox
                var outbox = new PaymentOutboxEvent {
                    Id          = Guid.NewGuid(),
                    Type        = "PaymentResult",
                    Payload     = JsonSerializer.Serialize(response),
                    OccurredAt  = DateTime.UtcNow,
                    IsProcessed = false
                };
                db.PaymentOutboxEvents.Add(outbox);
                await db.SaveChangesAsync(stoppingToken);
            };

            _channel.BasicConsume(queue: QueueName,
                                  autoAck: true,
                                  consumer: consumer);

            // держим сервис живым
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            base.Dispose();
        }
    }

    public class OrderPaymentRequest
    {
        public required string OrderId { get; set; }
        public required string UserId  { get; set; }
        public decimal Amount         { get; set; }
    }

    public class PaymentResponse
    {
        public required string OrderId { get; set; }
        public required string Status  { get; set; }
        public required string UserId  { get; set; }
    }
}
