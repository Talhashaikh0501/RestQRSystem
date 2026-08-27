using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.Kitchen.Controllers
{
    [Area("Kitchen")]
    [Authorize(Roles = "Kitchen")]
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

        // =========================================================
        // CURRENT KITCHEN USER RESTAURANT
        // =========================================================

        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            return user?.RestaurantId;
        }

        // =========================================================
        // LOAD RESTAURANT CATEGORIES
        // =========================================================

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

        // =========================================================
        // SAVE IMAGE
        // =========================================================

        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
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
                new FileStream(
                    filePath,
                    FileMode.Create);

            await image.CopyToAsync(stream);

            return $"/uploads/menu/{fileName}";
        }

        // =========================================================
        // VALIDATE MENU OPTIONS
        // =========================================================

        private bool ValidateOptions(MenuItemViewModel model)
        {
            model.Options ??=
                new List<MenuItemOptionViewModel>();

            model.Options = model.Options
                .Where(o =>
                    !string.IsNullOrWhiteSpace(o.Name) ||
                    o.Price > 0)
                .ToList();

            if (!model.Options.Any())
            {
                ModelState.AddModelError(
                    nameof(model.Options),
                    "Please add at least one serving option.");

                return false;
            }

            for (var i = 0; i < model.Options.Count; i++)
            {
                var option = model.Options[i];

                if (string.IsNullOrWhiteSpace(option.Name))
                {
                    ModelState.AddModelError(
                        $"Options[{i}].Name",
                        "Option name is required.");
                }

                if (option.Name?.Length > 100)
                {
                    ModelState.AddModelError(
                        $"Options[{i}].Name",
                        "Option name cannot exceed 100 characters.");
                }

                if (option.Price <= 0)
                {
                    ModelState.AddModelError(
                        $"Options[{i}].Price",
                        "Option price must be greater than 0.");
                }
            }

            return ModelState.IsValid;
        }

        // =========================================================
        // INDEX
        // /Kitchen/MenuItem
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var items = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m =>
                    m.Category != null &&
                    m.Category.RestaurantId ==
                    restaurantId.Value)
                .OrderBy(m => m.Category!.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return View(
                "~/Areas/Kitchen/Views/MenuItem/Index.cshtml",
                items);
        }

        // =========================================================
        // CREATE GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var model =
                new MenuItemViewModel();

            model.Options.Add(
                new MenuItemOptionViewModel
                {
                    DisplayOrder = 1,
                    IsAvailable = true
                });

            await LoadCategoriesAsync(
                model,
                restaurantId.Value);

            return View(
                "~/Areas/Kitchen/Views/MenuItem/Create.cshtml",
                model);
        }

        // =========================================================
        // CREATE POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            MenuItemViewModel model)
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var categoryExists =
                await _context.Categories.AnyAsync(c =>
                    c.Id == model.CategoryId &&
                    c.RestaurantId ==
                    restaurantId.Value);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Invalid category.");
            }

            ValidateOptions(model);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(
                    model,
                    restaurantId.Value);

                return View(
                    "~/Areas/Kitchen/Views/MenuItem/Create.cshtml",
                    model);
            }

            string? imageUrl;

            try
            {
                imageUrl =
                    await SaveImageAsync(model.Image);
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
                    "~/Areas/Kitchen/Views/MenuItem/Create.cshtml",
                    model);
            }

            var firstOptionPrice =
                model.Options
                    .OrderBy(o => o.DisplayOrder)
                    .First()
                    .Price;

            var menuItem =
                new MenuItem
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = firstOptionPrice,
                    CategoryId = model.CategoryId,
                    IsAvailable = model.IsAvailable,
                    ImageUrl = imageUrl
                };

            _context.MenuItems.Add(menuItem);

            await _context.SaveChangesAsync();

            var options = model.Options
                .OrderBy(o => o.DisplayOrder)
                .Select((option, index) =>
                    new MenuItemOption
                    {
                        MenuItemId = menuItem.Id,

                        Name =
                            option.Name.Trim(),

                        Price =
                            option.Price,

                        DisplayOrder =
                            index + 1,

                        IsAvailable =
                            option.IsAvailable
                    })
                .ToList();

            _context.MenuItemOptions.AddRange(options);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Menu item created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Options)
                .FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.Category != null &&
                    m.Category.RestaurantId ==
                    restaurantId.Value);

            if (item == null)
            {
                return NotFound();
            }

            var model =
                new MenuItemViewModel
                {
                    Id = item.Id,

                    Name = item.Name,

                    Description =
                        item.Description,

                    Price =
                        item.Price,

                    CategoryId =
                        item.CategoryId,

                    IsAvailable =
                        item.IsAvailable,

                    ExistingImageUrl =
                        item.ImageUrl,

                    Options = item.Options
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o =>
                            new MenuItemOptionViewModel
                            {
                                Id = o.Id,

                                Name =
                                    o.Name,

                                Price =
                                    o.Price,

                                DisplayOrder =
                                    o.DisplayOrder,

                                IsAvailable =
                                    o.IsAvailable
                            })
                        .ToList()
                };

            if (!model.Options.Any())
            {
                model.Options.Add(
                    new MenuItemOptionViewModel
                    {
                        Name =
                            string.Empty,

                        Price =
                            item.Price,

                        DisplayOrder =
                            1,

                        IsAvailable =
                            true
                    });
            }

            await LoadCategoriesAsync(
                model,
                restaurantId.Value);

            return View(
                "~/Areas/Kitchen/Views/MenuItem/Edit.cshtml",
                model);
        }

        // =========================================================
        // EDIT POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            MenuItemViewModel model)
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Options)
                .FirstOrDefaultAsync(m =>
                    m.Id == model.Id &&
                    m.Category != null &&
                    m.Category.RestaurantId ==
                    restaurantId.Value);

            if (item == null)
            {
                return NotFound();
            }

            var validCategory =
                await _context.Categories.AnyAsync(c =>
                    c.Id == model.CategoryId &&
                    c.RestaurantId ==
                    restaurantId.Value);

            if (!validCategory)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Invalid category.");
            }

            ValidateOptions(model);

            if (!ModelState.IsValid)
            {
                model.ExistingImageUrl =
                    item.ImageUrl;

                await LoadCategoriesAsync(
                    model,
                    restaurantId.Value);

                return View(
                    "~/Areas/Kitchen/Views/MenuItem/Edit.cshtml",
                    model);
            }

            if (model.Image != null)
            {
                try
                {
                    var newImageUrl =
                        await SaveImageAsync(
                            model.Image);

                    item.ImageUrl =
                        newImageUrl;
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
                        "~/Areas/Kitchen/Views/MenuItem/Edit.cshtml",
                        model);
                }
            }

            item.Name =
                model.Name;

            item.Description =
                model.Description;

            item.CategoryId =
                model.CategoryId;

            item.IsAvailable =
                model.IsAvailable;

            item.Price =
                model.Options
                    .OrderBy(o => o.DisplayOrder)
                    .First()
                    .Price;

            var submittedExistingIds =
                model.Options
                    .Where(o => o.Id > 0)
                    .Select(o => o.Id)
                    .ToHashSet();

            var optionsToDelete =
                item.Options
                    .Where(o =>
                        !submittedExistingIds.Contains(
                            o.Id))
                    .ToList();

            if (optionsToDelete.Any())
            {
                _context.MenuItemOptions.RemoveRange(
                    optionsToDelete);
            }

            var orderedOptions =
                model.Options
                    .OrderBy(o => o.DisplayOrder)
                    .ToList();

            for (var i = 0;
                 i < orderedOptions.Count;
                 i++)
            {
                var submittedOption =
                    orderedOptions[i];

                var displayOrder =
                    i + 1;

                if (submittedOption.Id > 0)
                {
                    var existingOption =
                        item.Options.FirstOrDefault(
                            o =>
                                o.Id ==
                                submittedOption.Id);

                    if (existingOption != null)
                    {
                        existingOption.Name =
                            submittedOption.Name.Trim();

                        existingOption.Price =
                            submittedOption.Price;

                        existingOption.DisplayOrder =
                            displayOrder;

                        existingOption.IsAvailable =
                            submittedOption.IsAvailable;
                    }
                }
                else
                {
                    var newOption =
                        new MenuItemOption
                        {
                            MenuItemId =
                                item.Id,

                            Name =
                                submittedOption.Name.Trim(),

                            Price =
                                submittedOption.Price,

                            DisplayOrder =
                                displayOrder,

                            IsAvailable =
                                submittedOption.IsAvailable
                        };

                    _context.MenuItemOptions.Add(
                        newOption);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Menu item updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TOGGLE AVAILABILITY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(
            int id)
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.Category != null &&
                    m.Category.RestaurantId ==
                    restaurantId.Value);

            if (item == null)
            {
                return NotFound();
            }

            item.IsAvailable =
                !item.IsAvailable;

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"]
                == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,

                    id =
                        item.Id,

                    isAvailable =
                        item.IsAvailable,

                    text =
                        item.IsAvailable
                            ? "ON"
                            : "OFF"
                });
            }

            TempData["Success"] =
                $"{item.Name} is now " +
                $"{(item.IsAvailable
                    ? "Available"
                    : "Unavailable")}.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurantId =
                await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m =>
                    m.Id == id &&
                    m.Category != null &&
                    m.Category.RestaurantId ==
                    restaurantId.Value);

            if (item == null)
            {
                return NotFound();
            }

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