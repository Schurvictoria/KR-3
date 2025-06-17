using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Services
{
    public interface INotificationSender
    {
        Task SendStatusUpdate(string userId, string status);
    }

    public class NotificationSender : INotificationSender
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationSender(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendStatusUpdate(string userId, string status)
        {
            return _hubContext.Clients.Group(userId)
                                     .SendAsync("OrderStatusChanged", status);
        }
    }
}
