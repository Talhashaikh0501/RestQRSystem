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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.RestaurantId;
        }

        // ---------------------------------------------
        // ORDER LIST
        // ---------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index(string? status)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var query = _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.RestaurantId == restaurantId);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<OrderStatus>(
                    status,
                    true,
                    out var parsedStatus))
            {
                query = query.Where(o =>
                    o.Status == parsedStatus);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new AdminOrderViewModel
                {
                    Id = o.Id,

                    OrderNumber =
                        o.OrderNumber,

                    TableNumber =
                        o.RestaurantTable.TableNumber,

                    Status =
                        o.Status.ToString(),

                    Total =
                        o.Total,

                    CreatedAt =
                        o.CreatedAt,

                    ItemCount =
                        o.Items.Sum(i => i.Quantity)
                })
                .ToListAsync();

            ViewBag.CurrentStatus = status;

            return View(
                "~/Areas/Admin/Views/Order/Index.cshtml",
                orders);
        }

        // ---------------------------------------------
        // ORDER DETAILS
        // ---------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Id == id &&
                    o.RestaurantId == restaurantId)
                .Select(o =>
                    new AdminOrderDetailsViewModel
                    {
                        Id = o.Id,

                        OrderNumber =
                            o.OrderNumber,

                        TableNumber =
                            o.RestaurantTable.TableNumber,

                        Status =
                            o.Status.ToString(),

                        Subtotal =
                            o.Subtotal,

                        Tax =
                            o.Tax,

                        Total =
                            o.Total,

                        CustomerNote =
                            o.CustomerNote,

                        CreatedAt =
                            o.CreatedAt,

                        UpdatedAt =
                            o.UpdatedAt,

                        Items = o.Items
                            .Select(i =>
                                new AdminOrderItemViewModel
                                {
                                    Name =
                                        i.ItemName,

                                    UnitPrice =
                                        i.UnitPrice,

                                    Quantity =
                                        i.Quantity,

                                    LineTotal =
                                        i.LineTotal
                                })
                            .ToList()
                    })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return View(
                "~/Areas/Admin/Views/Order/Details.cshtml",
                order);
        }
    }
}