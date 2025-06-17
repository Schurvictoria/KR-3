using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
        private readonly ILogger<NotificationConsumer> _logger;
        private const string QueueName = "notifications";

        public NotificationConsumer(ILogger<NotificationConsumer> logger, IConfiguration config)
        {
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
            int retries = 10;
            int delayMs = 3000;
            
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
                    _logger.LogError(ex, $"Failed to connect to RabbitMQ. Attempt {i + 1} of {retries}");
                    if (i == retries - 1) throw;
                    Thread.Sleep(delayMs);
                }
            }

            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/notificationHub")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("Subscribed", (message) => 
                _logger.LogInformation($"Hub subscription confirmed: {message}"));
            
            _hubConnection.On<string>("Unsubscribed", (message) => 
                _logger.LogInformation($"Hub unsubscription confirmed: {message}"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _hubConnection.StartAsync(stoppingToken);
                _logger.LogInformation("SignalR connection started");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var notification = JsonSerializer.Deserialize<NotificationMessage>(message);

                        if (notification != null)
                        {
                            _logger.LogInformation($"Sending notification to user {notification.UserId}: {notification.Message}");
                            await _hubConnection.InvokeAsync("SendNotification", 
                                notification.UserId, 
                                notification.Message,
                                stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notification message");
                    }
                };

                _channel.BasicConsume(QueueName, true, consumer);
                _logger.LogInformation("Started consuming messages from RabbitMQ");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification consumer");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping notification consumer");
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