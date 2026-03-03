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
    public class CategoriesController : Controller
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Product> _productRepository;

        public CategoriesController(
            IRepository<Category> categoryRepository,
            IRepository<Product> productRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        // GET: Categories with Search
        public async Task<IActionResult> Index(string searchTerm)
        {
            ViewData["CurrentSearch"] = searchTerm;

            IEnumerable<Category> categories;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                categories = await _categoryRepository.GetAllAsync();
            }
            else
            {
                searchTerm = searchTerm.ToLower().Trim();
                categories = await _categoryRepository.FindAsync(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchTerm)));
            }

            // Get product count for each category
            var categoriesWithCount = categories.ToList();
            foreach (var category in categoriesWithCount)
            {
                var products = await _productRepository.FindAsync(p => p.CategoryId == category.Id);
                ViewData[$"ProductCount_{category.Id}"] = products.Count();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ResultsCount = categoriesWithCount.Count;

            return View(categoriesWithCount.OrderBy(c => c.Name));
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _categoryRepository.GetByIdAsync(id.Value);
            if (category == null)
            {
                return NotFound();
            }

            var products = await _productRepository.FindAsync(p => p.CategoryId == id);
            ViewBag.ProductCount = products.Count();
            ViewBag.Products = products.Take(5); // Show first 5 products

            return View(category);
        }

        // GET: Categories/Create
        [Authorize(Policy = "ManagerAndAbove")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Check if category name already exists
                var existingCategory = await _categoryRepository.FindAsync(c =>
                    c.Name.ToLower() == category.Name.ToLower());

                if (existingCategory.Any())
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");
                    return View(category);
                }

                category.CreatedAt = DateTime.UtcNow;
                await _categoryRepository.AddAsync(category);
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _categoryRepository.GetByIdAsync(id.Value);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerAndAbove")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CreatedAt")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if name is taken by another category
                    var existingCategory = await _categoryRepository.FindAsync(c =>
                        c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);

                    if (existingCategory.Any())
                    {
                        ModelState.AddModelError("Name", "This name is already used by another category.");
                        return View(category);
                    }

                    category.UpdatedAt = DateTime.UtcNow;
                    await _categoryRepository.UpdateAsync(category);
                    TempData["Success"] = "Category updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _categoryRepository.ExistsAsync(category.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _categoryRepository.GetByIdAsync(id.Value);
            if (category == null)
            {
                return NotFound();
            }

            var products = await _productRepository.FindAsync(p => p.CategoryId == id);
            ViewBag.ProductCount = products.Count();

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category != null)
            {
                var products = await _productRepository.FindAsync(p => p.CategoryId == id);
                if (products.Any())
                {
                    TempData["Error"] = "Cannot delete category with existing products.";
                    return RedirectToAction(nameof(Index));
                }

                await _categoryRepository.DeleteAsync(category);
                TempData["Success"] = "Category deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}