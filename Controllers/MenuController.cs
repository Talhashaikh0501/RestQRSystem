using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    [AllowAnonymous]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Example:
        // /Menu/Scan/7eaaf02321c84a96ba5d79da473e0bc4
        [HttpGet]
        public async Task<IActionResult> Scan(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // Find the table using its QR token.
            // Inactive tables or restaurants cannot be used.
            var table = await _context.RestaurantTables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t =>
                    t.QRToken == id &&
                    t.IsActive &&
                    t.Restaurant.IsActive);

            if (table == null)
            {
                return NotFound();
            }

            // Load only this restaurant's active categories
            // and available menu items.
            var categories = await _context.Categories
                .Where(c =>
                    c.RestaurantId == table.RestaurantId &&
                    c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new QRMenuCategoryViewModel
                {
                    Name = c.Name,

                    MenuItems = c.MenuItems
                        .Where(m => m.IsAvailable)
                        .OrderBy(m => m.Name)
                        .Select(m => new QRMenuItemViewModel
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Description = m.Description,
                            Price = m.Price,
                            ImageUrl = m.ImageUrl
                        })
                        .ToList()
                })
                .ToListAsync();

            // Don't show empty categories.
            categories = categories
                .Where(c => c.MenuItems.Count > 0)
                .ToList();

            var model = new QRMenuViewModel
            {
                RestaurantId = table.RestaurantId,

                RestaurantName =
                    table.Restaurant.Name,

                TableId = table.Id,

                TableNumber =
                    table.TableNumber,

                QRToken =
                    table.QRToken,

                Categories = categories
            };

            return View(
                "~/Views/Menu/Index.cshtml",
                model);
        }
    }
}