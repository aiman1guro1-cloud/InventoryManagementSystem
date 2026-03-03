using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Web.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IRepository<Product> _productRepository;

        public SuppliersController(
            IRepository<Supplier> supplierRepository,
            IRepository<Product> productRepository)
        {
            _supplierRepository = supplierRepository;
            _productRepository = productRepository;
        }

        // GET: Suppliers with Search
        public async Task<IActionResult> Index(string searchTerm)
        {
            ViewData["CurrentSearch"] = searchTerm;

            IEnumerable<Supplier> suppliers;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                suppliers = await _supplierRepository.GetAllAsync();
            }
            else
            {
                searchTerm = searchTerm.ToLower().Trim();
                suppliers = await _supplierRepository.FindAsync(s =>
                    s.Name.ToLower().Contains(searchTerm) ||
                    s.Email.ToLower().Contains(searchTerm) ||
                    (s.Phone != null && s.Phone.Contains(searchTerm)) ||
                    (s.Address != null && s.Address.ToLower().Contains(searchTerm)));
            }

            // Get product count for each supplier
            var suppliersWithCount = suppliers.ToList();
            foreach (var supplier in suppliersWithCount)
            {
                var products = await _productRepository.FindAsync(p => p.SupplierId == supplier.Id);
                ViewData[$"ProductCount_{supplier.Id}"] = products.Count();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ResultsCount = suppliersWithCount.Count;

            return View(suppliersWithCount.OrderBy(s => s.Name));
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _supplierRepository.GetByIdAsync(id.Value);
            if (supplier == null)
            {
                return NotFound();
            }

            var products = await _productRepository.FindAsync(p => p.SupplierId == id);
            ViewBag.ProductCount = products.Count();
            ViewBag.Products = products.Take(5); // Show first 5 products

            return View(supplier);
        }

        // GET: Suppliers/Create
        [Authorize(Policy = "ManagerAndAbove")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingSupplier = await _supplierRepository
                    .FindAsync(s => s.Email.ToLower() == supplier.Email.ToLower());

                if (existingSupplier.Any())
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(supplier);
                }

                supplier.CreatedAt = DateTime.UtcNow;
                await _supplierRepository.AddAsync(supplier);
                TempData["Success"] = "Supplier created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _supplierRepository.GetByIdAsync(id.Value);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone,Address,CreatedAt")] Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email is taken by another supplier
                    var existingSupplier = await _supplierRepository
                        .FindAsync(s => s.Email.ToLower() == supplier.Email.ToLower() && s.Id != supplier.Id);

                    if (existingSupplier.Any())
                    {
                        ModelState.AddModelError("Email", "This email is already used by another supplier.");
                        return View(supplier);
                    }

                    supplier.UpdatedAt = DateTime.UtcNow;
                    await _supplierRepository.UpdateAsync(supplier);
                    TempData["Success"] = "Supplier updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _supplierRepository.ExistsAsync(supplier.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _supplierRepository.GetByIdAsync(id.Value);
            if (supplier == null)
            {
                return NotFound();
            }

            var products = await _productRepository.FindAsync(p => p.SupplierId == id);
            ViewBag.ProductCount = products.Count();

            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);
            if (supplier != null)
            {
                var products = await _productRepository.FindAsync(p => p.SupplierId == id);
                if (products.Any())
                {
                    TempData["Error"] = "Cannot delete supplier with existing products.";
                    return RedirectToAction(nameof(Index));
                }

                await _supplierRepository.DeleteAsync(supplier);
                TempData["Success"] = "Supplier deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}