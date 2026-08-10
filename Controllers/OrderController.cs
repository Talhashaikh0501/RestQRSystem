using Microsoft.AspNetCore.SignalR;
using RestaurantQR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Helpers;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    [AllowAnonymous]
    public class OrderController : Controller
    {
        private const string CartKey = "RestaurantQR_Cart";

        private readonly ApplicationDbContext _context;

        private readonly IHubContext<OrderHub> _orderHub;

        public OrderController(
    ApplicationDbContext context,
    IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _orderHub = orderHub;
        }

        private CartViewModel? GetCart()
        {
            return HttpContext.Session
                .GetObject<CartViewModel>(CartKey);
        }

        // ---------------------------------------------
        // CHECKOUT
        // ---------------------------------------------

        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction(
                    "Index",
                    "Cart");
            }

            var model = new CheckoutViewModel
            {
                Cart = cart
            };

            return View(model);
        }

        // ---------------------------------------------
        // PLACE ORDER
        // ---------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            CheckoutViewModel model)
        {
            var cart = GetCart();

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction(
                    "Index",
                    "Cart");
            }

            var table = await _context.RestaurantTables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t =>
                    t.Id == cart.TableId &&
                    t.RestaurantId == cart.RestaurantId &&
                    t.IsActive &&
                    t.Restaurant.IsActive);

            if (table == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This table is currently unavailable.");

                model.Cart = cart;

                return View(
                    "Checkout",
                    model);
            }

            var requestedIds = cart.Items
                .Select(i => i.MenuItemId)
                .Distinct()
                .ToList();

            var currentMenuItems =
                await _context.MenuItems
                    .Include(m => m.Category)
                    .Where(m =>
                        requestedIds.Contains(m.Id) &&
                        m.IsAvailable &&
                        m.Category.IsActive &&
                        m.Category.RestaurantId ==
                            cart.RestaurantId)
                    .ToListAsync();

            // If an item disappeared, was disabled,
            // or belongs to another restaurant:
            if (currentMenuItems.Count !=
                requestedIds.Count)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "One or more cart items are no longer available. Please return to the menu and update your cart.");

                model.Cart = cart;

                return View(
                    "Checkout",
                    model);
            }

            var sessionId =
                HttpContext.Session.Id;

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),

                TrackingToken = Guid.NewGuid().ToString("N"),

                RestaurantId =
                    cart.RestaurantId,

                RestaurantTableId =
                    cart.TableId,

                CustomerSessionId =
                    sessionId,

                Status =
                    OrderStatus.Pending,

                CustomerNote =
                    string.IsNullOrWhiteSpace(
                        model.CustomerNote)
                        ? null
                        : model.CustomerNote.Trim(),

                CreatedAt =
                    DateTime.UtcNow
            };

            decimal subtotal = 0;

            foreach (var cartItem in cart.Items)
            {
                var currentItem =
                    currentMenuItems.First(m =>
                        m.Id == cartItem.MenuItemId);

                // Defensive quantity validation.
                var quantity = Math.Clamp(
                    cartItem.Quantity,
                    1,
                    100);

                var lineTotal =
                    currentItem.Price * quantity;

                subtotal += lineTotal;

                order.Items.Add(
                    new OrderItem
                    {
                        MenuItemId =
                            currentItem.Id,

                        ItemName =
                            currentItem.Name,

                        UnitPrice =
                            currentItem.Price,

                        Quantity =
                            quantity,

                        LineTotal =
                            lineTotal
                    });
            }

            order.Subtotal = subtotal;

            // Tax is zero for now.
            // GST/tax rules can be added later.
            order.Tax = 0;

            order.Total =
                order.Subtotal + order.Tax;

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            // Notify this restaurant's Kitchen users
            // that a new order has arrived.
            await _orderHub.Clients
                .Group(OrderHub.GetRestaurantGroup(order.RestaurantId))
                .SendAsync(
                    "NewOrder",
                    new
                    {
                        orderId = order.Id,
                        orderNumber = order.OrderNumber
                    });

            // Clear cart after the order was saved.
            HttpContext.Session.Remove(CartKey);

            return RedirectToAction(
                nameof(Confirmation),
                new
                {
                    id = order.Id
                });
        }

        // ---------------------------------------------
        // CONFIRMATION
        // ---------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var sessionId =
                HttpContext.Session.Id;

            var order = await _context.Orders
                .Include(o => o.RestaurantTable)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.CustomerSessionId == sessionId);

            if (order == null)
            {
                return NotFound();
            }

            var model =
                new OrderConfirmationViewModel
                {

                    OrderId =
                        order.Id,

                    OrderNumber =
                        order.OrderNumber,

                    TableNumber =
                        order.RestaurantTable.TableNumber,

                    Total =
                        order.Total,

                    Status =
                        order.Status.ToString(),

                    TrackingToken = 
                        order.TrackingToken
                };

            return View(model);
        }

        private static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        }
    }
}