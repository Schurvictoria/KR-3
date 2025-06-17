using Microsoft.AspNetCore.SignalR.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotifiCationService.Services
{
    public class NotificationConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly HubConnection _hubConnection;
        private const string QueueName = "notifications";

        public NotificationConsumer()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            int retries = 10;
            int delayMs = 3000;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    break;
                }
                catch
                {
                    if (i == retries - 1) throw;
                    Thread.Sleep(delayMs);
                }
            }
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/notificationHub")
                .WithAutomaticReconnect()
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _hubConnection.StartAsync(stoppingToken);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var notification = JsonSerializer.Deserialize<NotificationMessage>(message);

                if (notification != null)
                {
                    await _hubConnection.InvokeAsync("SendNotification", 
                        notification.UserId, 
                        notification.Message,
                        stoppingToken);
                }
            };

            _channel.BasicConsume(QueueName, true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _hubConnection.DisposeAsync();
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