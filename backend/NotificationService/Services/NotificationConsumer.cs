using Microsoft.AspNetCore.SignalR.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.IO;

namespace NotifiCationService.Services
{
    public class NotificationConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly HubConnection _hubConnection;
        private const string QueueName = "notifications";
        private readonly string _fallbackFile = "notifications_fallback.jsonl";

        public NotificationConsumer()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/notificationHub")
                .WithAutomaticReconnect()
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await TryStartHubConnectionAsync(stoppingToken);
            await TrySendFallbackNotifications(stoppingToken);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var notification = JsonSerializer.Deserialize<NotificationMessage>(message);

                if (notification != null)
                {
                    if (await TrySendNotification(notification, stoppingToken) == false)
                    {
                        await SaveToFallbackFileAsync(notification);
                    }
                }
            };

            _channel.BasicConsume(QueueName, true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await TrySendFallbackNotifications(stoppingToken);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<bool> TrySendNotification(NotificationMessage notification, CancellationToken token)
        {
            try
            {
                await _hubConnection.InvokeAsync("SendNotification", notification.UserId, notification.Message, token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task TryStartHubConnectionAsync(CancellationToken token)
        {
            while (_hubConnection.State != HubConnectionState.Connected && !token.IsCancellationRequested)
            {
                try
                {
                    await _hubConnection.StartAsync(token);
                }
                catch
                {
                    await Task.Delay(5000, token);
                }
            }
        }

        private async Task SaveToFallbackFileAsync(NotificationMessage notification)
        {
            var line = JsonSerializer.Serialize(notification) + "\n";
            await File.AppendAllTextAsync(_fallbackFile, line);
        }

        private async Task TrySendFallbackNotifications(CancellationToken token)
        {
            if (!File.Exists(_fallbackFile)) return;
            var lines = await File.ReadAllLinesAsync(_fallbackFile, token);
            var toKeep = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var notification = JsonSerializer.Deserialize<NotificationMessage>(line);
                    if (notification != null && await TrySendNotification(notification, token))
                        continue;
                }
                catch { }
                toKeep.Add(line);
            }
            if (toKeep.Count == 0)
                File.Delete(_fallbackFile);
            else
                await File.WriteAllLinesAsync(_fallbackFile, toKeep, token);
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