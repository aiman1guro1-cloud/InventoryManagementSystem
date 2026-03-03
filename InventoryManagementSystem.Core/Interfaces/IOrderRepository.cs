using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;

namespace InventoryManagementSystem.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderFromCartAsync(string userId, string paymentMethod, string? notes = null);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<byte[]> GenerateReceiptPdfAsync(int orderId);
        string GenerateOrderNumber();
    }
}