using Microsoft.Extensions.Hosting;
using PaymentsService.Data;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace PaymentsService.Services
{
    public class PaymentOutboxPublisher : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private IConnection _connection;
        private IModel _channel;

        public PaymentOutboxPublisher(IServiceProvider serviceProvider, IConfiguration config)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            InitRabbitMqWithRetry();
        }

        private void InitRabbitMqWithRetry()
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "rabbitmq",
                Port = int.TryParse(_config["RabbitMQ:Port"], out var p) ? p : 5672,
                UserName = _config["RabbitMQ:User"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest"
            };
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.QueueDeclare("payment-results", durable: false, exclusive: false, autoDelete: false);
                    return;
                }
                catch
                {
                    Thread.Sleep(5000);
                    if (i == 9) throw;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var events = db.PaymentOutboxEvents.Where(e => !e.IsProcessed).ToList();
                foreach (var evt in events)
                {
                    try
                    {
                        var body = Encoding.UTF8.GetBytes(evt.Payload);
                        _channel.BasicPublish("", "payment-results", null, body);
                        evt.IsProcessed = true;
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        // логируйте ошибку, но не падайте
                    }
                }
                await Task.Delay(2000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
} 