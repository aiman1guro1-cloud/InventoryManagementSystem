using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Web.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            UserManager<ApplicationUser> userManager)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _cartRepository.GetCartWithItemsAsync(user.Id);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string paymentMethod, string notes)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                var order = await _orderRepository.CreateOrderFromCartAsync(user.Id, paymentMethod, notes);
                TempData["Success"] = $"Order placed successfully! Order Number: {order.OrderNumber}";

                // Redirect to receipt
                return RedirectToAction("Receipt", new { orderId = order.Id });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to place order: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        public async Task<IActionResult> Receipt(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            // Ensure user can only view their own receipts
            var user = await _userManager.GetUserAsync(User);
            if (order.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            return View(order);
        }

        public async Task<IActionResult> DownloadReceipt(int orderId)
        {
            try
            {
                var pdfBytes = await _orderRepository.GenerateReceiptPdfAsync(orderId);
                var order = await _orderRepository.GetOrderByIdAsync(orderId);

                return File(pdfBytes, "application/pdf", $"Receipt_{order.OrderNumber}.pdf");
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Failed to generate PDF: {ex.Message}";
                return RedirectToAction("Receipt", new { orderId });
            }
        }

        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _orderRepository.GetUserOrdersAsync(user.Id);
            return View(orders);
        }
    }
}