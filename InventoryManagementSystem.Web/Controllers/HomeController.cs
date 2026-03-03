using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.Web.Models;

namespace InventoryManagementSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRepository<Core.Entities.Product> _productRepository;
        private readonly IRepository<Core.Entities.Category> _categoryRepository;
        private readonly IRepository<Core.Entities.Supplier> _supplierRepository;

        public HomeController(
            IRepository<Core.Entities.Product> productRepository,
            IRepository<Core.Entities.Category> categoryRepository,
            IRepository<Core.Entities.Supplier> supplierRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _supplierRepository = supplierRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();
            var suppliers = await _supplierRepository.GetAllAsync();

            var viewModel = new DashboardViewModel
            {
                TotalProducts = products.Count(),
                TotalCategories = categories.Count(),
                TotalSuppliers = suppliers.Count(),
                LowStockProducts = products.Count(p => p.QuantityInStock <= p.MinimumStockLevel),
                RecentProducts = products.OrderByDescending(p => p.CreatedAt).Take(5).ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}