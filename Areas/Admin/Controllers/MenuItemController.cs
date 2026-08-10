using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "RestaurantAdmin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public MenuItemController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.RestaurantId;
        }

        private async Task LoadCategoriesAsync(
            MenuItemViewModel model,
            int restaurantId)
        {
            model.Categories = await _context.Categories
                .Where(c =>
                    c.RestaurantId == restaurantId &&
                    c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            var extension = Path
                .GetExtension(image.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Image size cannot exceed 5 MB.");
            }

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "menu");

            Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(
                uploadFolder,
                fileName);

            await using var stream =
                new FileStream(filePath, FileMode.Create);

            await image.CopyToAsync(stream);

            return $"/uploads/menu/{fileName}";
        }

        public async Task<IActionResult> Index()
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
                return Forbid();

            var items = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m =>
                    m.Category.RestaurantId == restaurantId)
                .OrderBy(m => m.Category.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return View(
                "~/Areas/Admin/Views/MenuItem/Index.cshtml",
                items);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
                return Forbid();

            var model = new MenuItemViewModel();

            await LoadCategoriesAsync(
                model,
                restaurantId.Value);

            return View(
                "~/Areas/Admin/Views/MenuItem/Create.cshtml",
                model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            MenuItemViewModel model)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
                return Forbid();

            var categoryExists =
                await _context.Categories.AnyAsync(c =>
                    c.Id == model.CategoryId &&
                    c.RestaurantId == restaurantId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Invalid category.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(
                    model,
                    restaurantId.Value);

                return View(
                    "~/Areas/Admin/Views/MenuItem/Create.cshtml",
                    model);
            }

            string? imageUrl;

            try
            {
                imageUrl = await SaveImageAsync(model.Image);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    nameof(model.Image),
                    ex.Message);

                await LoadCategoriesAsync(
                    model,
                    restaurantId.Value);

                return View(
                    "~/Areas/Admin/Views/MenuItem/Create.cshtml",
                    model);
            }

            var menuItem = new MenuItem
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                CategoryId = model.CategoryId,
                IsAvailable = model.IsAvailable,
                ImageUrl = imageUrl
            };

            _context.MenuItems.Add(menuItem);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Menu item created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
                return Forbid();

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.Category.RestaurantId == restaurantId);

            if (item == null)
                return NotFound();

            var model = new MenuItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                CategoryId = item.CategoryId,
                IsAvailable = item.IsAvailable,
                ExistingImageUrl = item.ImageUrl
            };

            await LoadCategoriesAsync(
                model,
                restaurantId.Value);

            return View(
                "~/Areas/Admin/Views/MenuItem/Edit.cshtml",
                model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            MenuItemViewModel model)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
                return Forbid();

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m =>
                    m.Id == model.Id &&
                    m.Category.RestaurantId == restaurantId);

            if (item == null)
                return NotFound();

            var validCategory =
                await _context.Categories.AnyAsync(c =>
                    c.Id == model.CategoryId &&
                    c.RestaurantId == restaurantId);

            if (!validCategory)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Invalid category.");
            }

            if (!ModelState.IsValid)
            {
                model.ExistingImageUrl = item.ImageUrl;

                await LoadCategoriesAsync(
                    model,
                    restaurantId.Value);

                return View(
                    "~/Areas/Admin/Views/MenuItem/Edit.cshtml",
                    model);
            }

            if (model.Image != null)
            {
                try
                {
                    var newImageUrl =
                        await SaveImageAsync(model.Image);

                    item.ImageUrl = newImageUrl;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(
                        nameof(model.Image),
                        ex.Message);

                    model.ExistingImageUrl =
                        item.ImageUrl;

                    await LoadCategoriesAsync(
                        model,
                        restaurantId.Value);

                    return View(
                        "~/Areas/Admin/Views/MenuItem/Edit.cshtml",
                        model);
                }
            }

            item.Name = model.Name;
            item.Description = model.Description;
            item.Price = model.Price;
            item.CategoryId = model.CategoryId;
            item.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Menu item updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.Category.RestaurantId == restaurantId);

            if (item == null)
            {
                return NotFound();
            }

            // Check whether this menu item exists
            // in any historical order.
            var hasOrderHistory =
                await _context.OrderItems
                    .AnyAsync(oi =>
                        oi.MenuItemId == item.Id);

            if (hasOrderHistory)
            {
                TempData["Error"] =
                    "This menu item has order history and cannot be deleted. Mark it unavailable instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.MenuItems.Remove(item);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Menu item deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        
    }
}