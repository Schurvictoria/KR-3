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
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "order-payments";

        public OrderPaymentConsumer(IServiceProvider serviceProvider, IConfiguration config)
        {
            _serviceProvider = serviceProvider;

            // Читаем параметры RabbitMQ из окружения (docker-compose)
            var rabbitHost = config["RabbitMQ:Host"] ?? "rabbitmq";
            var rabbitPort = int.TryParse(config["RabbitMQ:Port"], out var p) ? p : 5672;
            var rabbitUser = config["RabbitMQ:User"] ?? "guest";
            var rabbitPass = config["RabbitMQ:Password"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = rabbitHost,
                Port = (ushort)rabbitPort,
                UserName = rabbitUser,
                Password = rabbitPass,
                DispatchConsumersAsync = true
            };

            // Пытаемся подключиться с ретраями
            const int maxAttempts = 5;
            const int delayMs = 3000;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    Console.WriteLine($"RabbitMQ connection attempt {attempt} failed: {ex.Message}. Retrying in {delayMs}ms...");
                    Thread.Sleep(delayMs);
                }
                catch
                {
                    throw;
                }
            }

            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                var body    = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var order   = JsonSerializer.Deserialize<OrderPaymentRequest>(message);
                if (order == null) return;

                using var scope     = _serviceProvider.CreateScope();
                var dbContext       = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                var account = await dbContext.Accounts
                                             .FirstOrDefaultAsync(a => a.UserId == order.UserId, stoppingToken);
                if (account == null) return;

                if (account.Balance >= order.Amount)
                {
                    var originalRV = account.RowVersion;
                    account.Balance -= order.Amount;
                    try
                    {
                        dbContext.Entry(account).OriginalValues["RowVersion"] = originalRV;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        var success = new PaymentResponse
                        {
                            OrderId = order.OrderId,
                            Status  = "Success",
                            UserId  = order.UserId
                        };
                        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(success));
                        _channel.BasicPublish(
                            exchange: "",
                            routingKey: "payment-results",
                            basicProperties: null,
                            body: bytes);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // конфликт – пропускаем
                    }
                }
                else
                {
                    var failure = new PaymentResponse
                    {
                        OrderId = order.OrderId,
                        Status  = "Failed",
                        UserId  = order.UserId
                    };
                    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(failure));
                    _channel.BasicPublish(
                        exchange: "",
                        routingKey: "payment-results",
                        basicProperties: null,
                        body: bytes);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: true,
                consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }

    // DTO для запроса на списание
    public class OrderPaymentRequest
    {
        public required string OrderId { get; set; }
        public required string UserId  { get; set; }
        public decimal Amount         { get; set; }
    }

    // DTO для ответа после списания
    public class PaymentResponse
    {
        public required string OrderId { get; set; }
        public required string Status  { get; set; }
        public required string UserId  { get; set; }
    }
}
