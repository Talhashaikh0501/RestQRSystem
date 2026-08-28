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
    public class LiveOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LiveOrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // GET CURRENT ADMIN RESTAURANT
        // =========================================================

        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            return user?.RestaurantId;
        }

        // =========================================================
        // LIVE ORDERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            // Only active orders are displayed on Live Orders.
            // Completed and Cancelled orders remain available
            // inside the normal Admin Orders module.

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.RestaurantId == restaurantId.Value &&
                    o.Status != OrderStatus.Completed &&
                    o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new AdminOrderViewModel
                {
                    Id = o.Id,

                    OrderNumber = o.OrderNumber,

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

            return View(
                "~/Areas/Admin/Views/LiveOrder/Index.cshtml",
                orders);
        }
    }
}