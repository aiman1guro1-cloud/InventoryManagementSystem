using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using InventoryManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartRepository _cartRepository;
        private readonly IRepository<Product> _productRepository;

        public OrderRepository(
            ApplicationDbContext context,
            ICartRepository cartRepository,
            IRepository<Product> productRepository)
        {
            _context = context;
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        public string GenerateOrderNumber()
        {
            // Format: INV-YYYYMMDD-XXXX (e.g., INV-20250228-0001)
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var lastOrderToday = _context.Orders
                .Where(o => o.OrderNumber.StartsWith($"INV-{datePart}"))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefault();

            int sequence = 1;
            if (lastOrderToday != null)
            {
                var lastSeq = lastOrderToday.OrderNumber.Split('-').Last();
                sequence = int.Parse(lastSeq) + 1;
            }

            return $"INV-{datePart}-{sequence:D4}";
        }

        public async Task<Order> CreateOrderFromCartAsync(string userId, string paymentMethod, string? notes = null)
        {
            var cart = await _cartRepository.GetCartWithItemsAsync(userId);
            if (cart == null || !cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty");

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create order
                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    UserId = userId,
                    CustomerName = (await _context.Users.FindAsync(userId))?.UserName ?? "Customer",
                    CustomerEmail = (await _context.Users.FindAsync(userId))?.Email ?? "",
                    OrderDate = DateTime.UtcNow,
                    Subtotal = cart.Subtotal,
                    TaxAmount = cart.TaxAmount,
                    TotalAmount = cart.Total,
                    PaymentMethod = paymentMethod,
                    OrderStatus = "Completed",
                    Notes = notes
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Create order items and update stock
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        ProductSKU = cartItem.ProductSKU,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice
                    };
                    await _context.OrderItems.AddAsync(orderItem);

                    // Update product stock
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        product.QuantityInStock -= cartItem.Quantity;
                        await _productRepository.UpdateAsync(product);
                    }
                }

                await _context.SaveChangesAsync();

                // Clear the cart
                await _cartRepository.ClearCartAsync(userId);

                // Commit transaction
                await transaction.CommitAsync();

                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.OrderStatus = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(int orderId)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found");

            // This will be implemented with PDF generation
            // For now, return HTML string as bytes
            var html = GenerateReceiptHtml(order);
            return Encoding.UTF8.GetBytes(html);
        }

        private string GenerateReceiptHtml(Order order)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            sb.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
            sb.AppendLine(".company-name { font-size: 24px; font-weight: bold; color: #0d6efd; }");
            sb.AppendLine(".receipt-title { font-size: 20px; margin: 20px 0; }");
            sb.AppendLine(".order-info { margin: 20px 0; padding: 10px; background: #f8f9fa; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            sb.AppendLine("th { background: #0d6efd; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("td { padding: 10px; border-bottom: 1px solid #ddd; }");
            sb.AppendLine(".totals { text-align: right; margin-top: 20px; }");
            sb.AppendLine(".total-line { font-size: 18px; margin: 5px 0; }");
            sb.AppendLine(".grand-total { font-size: 22px; font-weight: bold; color: #0d6efd; }");
            sb.AppendLine(".footer { text-align: center; margin-top: 50px; color: #6c757d; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<div class='company-name'>INVENTORY MANAGEMENT SYSTEM</div>");
            sb.AppendLine("<div>123 Business Street, City, State 12345</div>");
            sb.AppendLine("<div>Phone: (123) 456-7890 | Email: sales@inventory.com</div>");
            sb.AppendLine("</div>");

            // Receipt Title
            sb.AppendLine("<div class='receipt-title'>SALES RECEIPT</div>");

            // Order Info
            sb.AppendLine("<div class='order-info'>");
            sb.AppendLine($"<p><strong>Receipt #:</strong> {order.OrderNumber}</p>");
            sb.AppendLine($"<p><strong>Date:</strong> {order.OrderDate:MMMM dd, yyyy hh:mm tt}</p>");
            sb.AppendLine($"<p><strong>Customer:</strong> {order.CustomerName}</p>");
            sb.AppendLine($"<p><strong>Email:</strong> {order.CustomerEmail}</p>");
            sb.AppendLine($"<p><strong>Payment Method:</strong> {order.PaymentMethod}</p>");
            sb.AppendLine("</div>");

            // Items Table
            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Item</th>");
            sb.AppendLine("<th>SKU</th>");
            sb.AppendLine("<th>Qty</th>");
            sb.AppendLine("<th>Unit Price</th>");
            sb.AppendLine("<th>Total</th>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in order.OrderItems)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{item.ProductName}</td>");
                sb.AppendLine($"<td>{item.ProductSKU}</td>");
                sb.AppendLine($"<td>{item.Quantity}</td>");
                sb.AppendLine($"<td>{item.UnitPrice:C}</td>");
                sb.AppendLine($"<td>{item.TotalPrice:C}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            // Totals
            sb.AppendLine("<div class='totals'>");
            sb.AppendLine($"<div class='total-line'>Subtotal: {order.Subtotal:C}</div>");
            sb.AppendLine($"<div class='total-line'>Tax (10%): {order.TaxAmount:C}</div>");
            sb.AppendLine($"<div class='grand-total'>Total: {order.TotalAmount:C}</div>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Thank you for your business!</p>");
            sb.AppendLine("<p>This is a computer-generated receipt. No signature required.</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}