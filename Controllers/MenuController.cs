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

        // =====================================================
        // SCAN QR CODE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Scan(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

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

            // =================================================
            // LOAD RESTAURANT MENU
            // =================================================

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

                            ImageUrl = m.ImageUrl,

                            Options = m.Options
                                .Where(o => o.IsAvailable)
                                .OrderBy(o => o.DisplayOrder)
                                .Select(o =>
                                    new QRMenuItemOptionViewModel
                                    {
                                        Id = o.Id,
                                        Name = o.Name,
                                        Price = o.Price,
                                        DisplayOrder =
                                            o.DisplayOrder,
                                        IsAvailable =
                                            o.IsAvailable
                                    })
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync();

            // =================================================
            // REMOVE EMPTY CATEGORIES
            // =================================================

            categories = categories
                .Where(c => c.MenuItems.Count > 0)
                .ToList();

            // =================================================
            // CREATE VIEW MODEL
            // =================================================

            var model = new QRMenuViewModel
            {
                RestaurantId =
                    table.RestaurantId,

                RestaurantName =
                    table.Restaurant.Name,

                TableId =
                    table.Id,

                TableNumber =
                    table.TableNumber,

                QRToken =
                    table.QRToken,

                Categories =
                    categories
            };

            return View(
                "~/Views/Menu/Index.cshtml",
                model);
        }
    }
}