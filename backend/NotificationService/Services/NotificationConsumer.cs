using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NotificationService.Hubs;

namespace NotificationService.Services
{
    public class NotificationConsumer : BackgroundService
    {
        private readonly INotificationSender _notificationSender;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<NotificationConsumer> _logger;
        private const string QueueName = "notifications";

        public NotificationConsumer(
            INotificationSender notificationSender,
            ILogger<NotificationConsumer> logger,
            IConfiguration config)
        {
            _notificationSender = notificationSender;
            _logger = logger;

            var rabbitHost = config["RabbitMQ:Host"] ?? "rabbitmq";
            var rabbitPort = int.Parse(config["RabbitMQ:Port"] ?? "5672");
            var rabbitUser = config["RabbitMQ:User"] ?? "guest";
            var rabbitPass = config["RabbitMQ:Password"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = rabbitHost,
                Port = rabbitPort,
                UserName = rabbitUser,
                Password = rabbitPass,
                DispatchConsumersAsync = true
            };

            int retries = 10, delayMs = 3000;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _logger.LogInformation("Successfully connected to RabbitMQ");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to RabbitMQ. Attempt {i + 1}/{retries}");
                    if (i == retries - 1) throw;
                    Thread.Sleep(delayMs);
                }
            }

            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var notification = JsonSerializer.Deserialize<NotificationMessage>(message);
                    if (notification != null)
                    {
                        _logger.LogInformation($"Sending notification to user {notification.UserId}");
                        await _notificationSender.SendStatusUpdate(notification.UserId, notification.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification message");
                }
            };

            _channel.BasicConsume(QueueName, autoAck: true, consumer);
            _logger.LogInformation("Started consuming messages from RabbitMQ");

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping notification consumer");
            _channel.Dispose();
            _connection.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }

    public class NotificationMessage
    {
        public required string UserId { get; set; }
        public required string Message { get; set; }
    }
}
