using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;

namespace InventoryManagementSystem.Core.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetActiveCartAsync(string userId);
        Task<Cart> CreateCartAsync(string userId);
        Task<CartItem?> AddToCartAsync(string userId, int productId, int quantity);
        Task<bool> RemoveFromCartAsync(int cartItemId);
        Task<bool> UpdateQuantityAsync(int cartItemId, int quantity);
        Task<Cart?> ClearCartAsync(string userId);
        Task<Cart?> GetCartWithItemsAsync(string userId);
    }
}