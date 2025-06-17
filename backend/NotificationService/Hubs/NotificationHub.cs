using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        if (exception != null)
        {
            _logger.LogError($"Disconnection error: {exception.Message}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Subscribe(string userId)
    {
        _logger.LogInformation($"User {userId} subscribing to notifications");
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await Clients.Caller.SendAsync("Subscribed", $"Successfully subscribed to notifications for user {userId}");
    }

    public async Task Unsubscribe(string userId)
    {
        _logger.LogInformation($"User {userId} unsubscribing from notifications");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        await Clients.Caller.SendAsync("Unsubscribed", $"Successfully unsubscribed from notifications for user {userId}");
    }
}
