using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using InventoryManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Product> _productRepository;

        public CartRepository(ApplicationDbContext context, IRepository<Product> productRepository)
        {
            _context = context;
            _productRepository = productRepository;
        }

        public async Task<Cart?> GetActiveCartAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }

        public async Task<Cart> CreateCartAsync(string userId)
        {
            var cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<CartItem?> AddToCartAsync(string userId, int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.QuantityInStock < quantity)
                return null;

            var cart = await GetActiveCartAsync(userId) ?? await CreateCartAsync(userId);

            var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.UnitPrice = product.Price; // Update price in case it changed
                _context.CartItems.Update(existingItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductSKU = product.SKU,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    AddedAt = DateTime.UtcNow
                };
                await _context.CartItems.AddAsync(cartItem);
            }

            await _context.SaveChangesAsync();
            return await GetCartWithItemsAsync(userId) != null ?
                (await GetCartWithItemsAsync(userId))?.CartItems?.FirstOrDefault(ci => ci.ProductId == productId) : null;
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateQuantityAsync(int cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (item == null || item.Product == null || item.Product.QuantityInStock < quantity)
                return false;

            item.Quantity = quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Cart?> ClearCartAsync(string userId)
        {
            var cart = await GetActiveCartAsync(userId);
            if (cart == null) return null;

            _context.CartItems.RemoveRange(cart.CartItems ?? new List<CartItem>());
            cart.IsActive = false;
            await _context.SaveChangesAsync();

            return await CreateCartAsync(userId);
        }

        public async Task<Cart?> GetCartWithItemsAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }
    }
}