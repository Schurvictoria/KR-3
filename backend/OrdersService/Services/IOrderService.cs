using System;
using System.Threading.Tasks;

namespace OrdersService.Services
{
    public interface IOrderService
    {
        Task<Guid> CreateOrderAsync(string product, decimal amount);
    }
}
