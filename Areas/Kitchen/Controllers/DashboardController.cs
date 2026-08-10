using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Hubs;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.Kitchen.Controllers
{
    [Area("Kitchen")]
    [Authorize(Roles = "Kitchen")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<OrderHub> _orderHub;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _userManager = userManager;
            _orderHub = orderHub;
        }

        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.RestaurantId;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.RestaurantId == restaurantId &&
                    o.Status != OrderStatus.Completed &&
                    o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new KitchenOrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TableNumber = o.RestaurantTable.TableNumber,
                    Status = o.Status.ToString(),
                    CustomerNote = o.CustomerNote,
                    CreatedAt = o.CreatedAt,
                    Items = o.Items
                        .Select(i => new KitchenOrderItemViewModel
                        {
                            Name = i.ItemName,
                            Quantity = i.Quantity
                        })
                        .ToList()
                })
                .ToListAsync();

            return View(
                "~/Areas/Kitchen/Views/Dashboard/Index.cshtml",
                orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.RestaurantId == restaurantId);

            if (order == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                OrderStatus.Accepted,
                OrderStatus.Preparing,
                OrderStatus.Ready,
                OrderStatus.Completed
            };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest();
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _orderHub.Clients
                .Group(OrderHub.GetOrderGroup(order.TrackingToken))
                .SendAsync(
                    "OrderStatusChanged",
                    new
                    {
                        orderId = order.Id,
                        status = order.Status.ToString()
                    });

            return RedirectToAction(nameof(Index));
        }
    }
}