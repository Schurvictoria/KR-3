using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    // Можно добавить методы для авторизации или подписки на определённые заказы/пользователей

    public Task Subscribe(string userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
