using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Web.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ICartRepository cartRepository, UserManager<ApplicationUser> userManager)
        {
            _cartRepository = cartRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _cartRepository.GetCartWithItemsAsync(user.Id);

            return View(cart ?? new Cart());
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _cartRepository.AddToCartAsync(user.Id, productId, quantity);

            if (result != null)
            {
                TempData["Success"] = "Item added to cart!";
            }
            else
            {
                TempData["Error"] = "Failed to add item to cart. Check stock availability.";
            }

            return RedirectToAction("Index", "Products");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var result = await _cartRepository.UpdateQuantityAsync(cartItemId, quantity);

            if (result)
            {
                TempData["Success"] = "Cart updated!";
            }
            else
            {
                TempData["Error"] = "Failed to update quantity. Check stock.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            await _cartRepository.RemoveFromCartAsync(cartItemId);
            TempData["Success"] = "Item removed from cart!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var user = await _userManager.GetUserAsync(User);
            await _cartRepository.ClearCartAsync(user.Id);
            TempData["Success"] = "Cart cleared!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _cartRepository.GetCartWithItemsAsync(user.Id);

            if (cart == null)
                return Json(new { totalItems = 0, subtotal = 0 });

            return Json(new
            {
                totalItems = cart.TotalItems,
                subtotal = cart.Subtotal,
                tax = cart.TaxAmount,
                total = cart.Total
            });
        }
    }
}