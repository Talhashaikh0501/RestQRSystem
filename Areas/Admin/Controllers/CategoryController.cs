using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "RestaurantAdmin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoryController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper: gets the logged-in admin's restaurant
        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.RestaurantId;
        }

        // GET: /Admin/Category
        public async Task<IActionResult> Index()
        {
            var restaurantId = await GetRestaurantIdAsync();
            if (restaurantId == null) return Forbid();

            var categories = await _context.Categories
                .Where(c => c.RestaurantId == restaurantId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(
     "~/Areas/Admin/Views/Category/Index.cshtml",
     categories);
        }

        // GET: /Admin/Category/Create
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        // POST: /Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            var restaurantId = await GetRestaurantIdAsync();
            if (restaurantId == null) return Forbid();

            if (!ModelState.IsValid) return View(model);

            var category = new Category
            {
                Name = model.Name,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                RestaurantId = restaurantId.Value   // tenant enforced here
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();
            if (restaurantId == null) return Forbid();

            // The RestaurantId check prevents editing another restaurant's data
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id && c.RestaurantId == restaurantId);

            if (category == null) return NotFound();

            var model = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive
            };

            return View(model);
        }

        // POST: /Admin/Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            var restaurantId = await GetRestaurantIdAsync();
            if (restaurantId == null) return Forbid();

            if (!ModelState.IsValid) return View(model);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == model.Id && c.RestaurantId == restaurantId);

            if (category == null) return NotFound();

            category.Name = model.Name;
            category.DisplayOrder = model.DisplayOrder;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();
            if (restaurantId == null) return Forbid();

            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c =>
                    c.Id == id && c.RestaurantId == restaurantId);

            if (category == null) return NotFound();

            if (category.MenuItems.Any())
            {
                TempData["Error"] =
                    "Cannot delete a category that contains menu items.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}