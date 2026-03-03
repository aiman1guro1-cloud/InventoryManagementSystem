using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Web.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Supplier> _supplierRepository;

        public ProductsController(
            IRepository<Product> productRepository,
            IRepository<Category> categoryRepository,
            IRepository<Supplier> supplierRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _supplierRepository = supplierRepository;
        }

        // GET: Products with Search
        public async Task<IActionResult> Index(string searchTerm, string searchField = "all", string sortOrder = "name")
        {
            // Store search parameters in ViewData for the view
            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentField"] = searchField;
            ViewData["CurrentSort"] = sortOrder;

            // Sort order indicators
            ViewData["NameSortParam"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["PriceSortParam"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["StockSortParam"] = sortOrder == "stock" ? "stock_desc" : "stock";

            IEnumerable<Product> products;

            // Apply search filter
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                products = await _productRepository.GetAllAsync();
            }
            else
            {
                searchTerm = searchTerm.ToLower().Trim();
                products = await _productRepository.FindAsync(GetProductSearchPredicate(searchTerm, searchField));
            }

            // Apply sorting
            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "price" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "stock" => products.OrderBy(p => p.QuantityInStock),
                "stock_desc" => products.OrderByDescending(p => p.QuantityInStock),
                _ => products.OrderBy(p => p.Name), // Default sort by name
            };

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ResultsCount = products.Count();

            return View(products);
        }

        private Expression<Func<Product, bool>> GetProductSearchPredicate(string searchTerm, string searchField)
        {
            return searchField.ToLower() switch
            {
                "name" => p => p.Name.ToLower().Contains(searchTerm),
                "sku" => p => p.SKU.ToLower().Contains(searchTerm),
                "category" => p => p.Category != null && p.Category.Name.ToLower().Contains(searchTerm),
                "supplier" => p => p.Supplier != null && p.Supplier.Name.ToLower().Contains(searchTerm),
                "description" => p => p.Description != null && p.Description.ToLower().Contains(searchTerm),
                _ => p => p.Name.ToLower().Contains(searchTerm) ||
                         p.SKU.ToLower().Contains(searchTerm) ||
                         (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                         (p.Category != null && p.Category.Name.ToLower().Contains(searchTerm)) ||
                         (p.Supplier != null && p.Supplier.Name.ToLower().Contains(searchTerm))
            };
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
            ViewBag.Suppliers = new SelectList(await _supplierRepository.GetAllAsync(), "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Create([Bind("Name,Description,SKU,Price,QuantityInStock,MinimumStockLevel,CategoryId,SupplierId")] Product product)
        {
            if (ModelState.IsValid)
            {
                // Check if SKU already exists
                var existingProduct = await _productRepository.FindAsync(p => p.SKU == product.SKU);
                if (existingProduct.Any())
                {
                    ModelState.AddModelError("SKU", "This SKU already exists.");
                    ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
                    ViewBag.Suppliers = new SelectList(await _supplierRepository.GetAllAsync(), "Id", "Name", product.SupplierId);
                    return View(product);
                }

                product.CreatedAt = DateTime.UtcNow;
                await _productRepository.AddAsync(product);
                TempData["Success"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Suppliers = new SelectList(await _supplierRepository.GetAllAsync(), "Id", "Name", product.SupplierId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Suppliers = new SelectList(await _supplierRepository.GetAllAsync(), "Id", "Name", product.SupplierId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,SKU,Price,QuantityInStock,MinimumStockLevel,CategoryId,SupplierId,CreatedAt")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if SKU is taken by another product
                    var existingProduct = await _productRepository.FindAsync(p => p.SKU == product.SKU && p.Id != product.Id);
                    if (existingProduct.Any())
                    {
                        ModelState.AddModelError("SKU", "This SKU is already used by another product.");
                        ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
                        ViewBag.Suppliers = new SelectList(await _supplierRepository.GetAllAsync(), "Id", "Name", product.SupplierId);
                        return View(product);
                    }

                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);
                    TempData["Success"] = "Product updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _productRepository.ExistsAsync(product.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Suppliers = new SelectList(await _supplierRepository.GetAllAsync(), "Id", "Name", product.SupplierId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                await _productRepository.DeleteAsync(product);
                TempData["Success"] = "Product deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Export
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Export(string searchTerm)
        {
            // This will be for Excel export later
            TempData["Info"] = "Export feature coming soon!";
            return RedirectToAction(nameof(Index));
        }
    }
}