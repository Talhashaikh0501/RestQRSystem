using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Helpers;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    [AllowAnonymous]
    public class CartController : Controller
    {
        private const string CartKey = "RestaurantQR_Cart";

        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private CartViewModel? GetCart()
        {
            return HttpContext.Session
                .GetObject<CartViewModel>(CartKey);
        }

        private void SaveCart(CartViewModel cart)
        {
            HttpContext.Session
                .SetObject(CartKey, cart);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"]
                == "XMLHttpRequest";
        }

        private IActionResult CartError(
            string message,
            int statusCode = 400)
        {
            if (IsAjaxRequest())
            {
                return StatusCode(statusCode, new
                {
                    success = false,
                    message = message
                });
            }

            return BadRequest(message);
        }

        // =====================================================
        // CART PAGE
        // =====================================================

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();

            return View(
                cart ?? new CartViewModel());
        }

        // =====================================================
        // ADD ITEM
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> Add(
            int menuItemId,
            int optionId,
            string qrToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return CartError(
                    "QR code information is missing.");
            }

            // =================================================
            // VALIDATE TABLE
            // =================================================

            var table = await _context.RestaurantTables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t =>
                    t.QRToken == qrToken &&
                    t.IsActive &&
                    t.Restaurant.IsActive);

            if (table == null)
            {
                return CartError(
                    "This table is no longer available.",
                    404);
            }

            // =================================================
            // VALIDATE MENU ITEM
            // =================================================

            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Options)
                .FirstOrDefaultAsync(m =>
                    m.Id == menuItemId &&
                    m.IsAvailable &&
                    m.Category.IsActive &&
                    m.Category.RestaurantId ==
                        table.RestaurantId);

            if (menuItem == null)
            {
                return CartError(
                    "This menu item is no longer available.",
                    404);
            }

            // =================================================
            // PRICE / OPTION
            // =================================================

            var actualOptionId = 0;
            var actualOptionName = string.Empty;
            var actualPrice = menuItem.Price;

            // If this item has serving options,
            // validate the selected option.
            if (menuItem.Options.Any())
            {
                var selectedOption =
                    menuItem.Options.FirstOrDefault(o =>
                        o.Id == optionId &&
                        o.IsAvailable);

                if (selectedOption == null)
                {
                    return CartError(
                        "Please select a valid serving option.");
                }

                actualOptionId =
                    selectedOption.Id;

                actualOptionName =
                    selectedOption.Name;

                actualPrice =
                    selectedOption.Price;
            }

            // =================================================
            // GET / CREATE CART
            // =================================================

            var cart = GetCart();

            if (cart == null ||
                cart.RestaurantId != table.RestaurantId ||
                cart.TableId != table.Id)
            {
                cart = new CartViewModel
                {
                    RestaurantId =
                        table.RestaurantId,

                    TableId =
                        table.Id,

                    TableNumber =
                        table.TableNumber,

                    QRToken =
                        table.QRToken
                };
            }

            // =================================================
            // SAME ITEM + SAME OPTION = SAME CART ROW
            // =================================================

            var existingItem = cart.Items
                .FirstOrDefault(i =>
                    i.MenuItemId == menuItem.Id &&
                    i.OptionId == actualOptionId);

            if (existingItem == null)
            {
                cart.Items.Add(
                    new CartItemViewModel
                    {
                        MenuItemId =
                            menuItem.Id,

                        OptionId =
                            actualOptionId,

                        Name =
                            menuItem.Name,

                        OptionName =
                            actualOptionName,

                        Price =
                            actualPrice,

                        Quantity =
                            1,

                        ImageUrl =
                            menuItem.ImageUrl
                    });
            }
            else
            {
                existingItem.Quantity++;
            }

            SaveCart(cart);

            // =================================================
            // AJAX RESPONSE
            // =================================================

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    totalQuantity =
                        cart.TotalQuantity,
                    totalPrice =
                        cart.Subtotal
                });
            }

            return RedirectToAction(
                "Scan",
                "Menu",
                new
                {
                    id = qrToken
                });
        }

        // =====================================================
        // INCREASE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Increase(
            int menuItemId,
            int optionId)
        {
            var cart = GetCart();

            if (cart != null)
            {
                var item = cart.Items
                    .FirstOrDefault(i =>
                        i.MenuItemId == menuItemId &&
                        i.OptionId == optionId);

                if (item != null)
                {
                    item.Quantity++;
                    SaveCart(cart);
                }
            }

            return RedirectToAction(
                nameof(Index));
        }

        // =====================================================
        // DECREASE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(
            int menuItemId,
            int optionId)
        {
            var cart = GetCart();

            if (cart != null)
            {
                var item = cart.Items
                    .FirstOrDefault(i =>
                        i.MenuItemId == menuItemId &&
                        i.OptionId == optionId);

                if (item != null)
                {
                    item.Quantity--;

                    if (item.Quantity <= 0)
                    {
                        cart.Items.Remove(item);
                    }

                    SaveCart(cart);
                }
            }

            return RedirectToAction(
                nameof(Index));
        }

        // =====================================================
        // REMOVE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(
            int menuItemId,
            int optionId)
        {
            var cart = GetCart();

            if (cart != null)
            {
                var item = cart.Items
                    .FirstOrDefault(i =>
                        i.MenuItemId == menuItemId &&
                        i.OptionId == optionId);

                if (item != null)
                {
                    cart.Items.Remove(item);
                    SaveCart(cart);
                }
            }

            return RedirectToAction(
                nameof(Index));
        }
    }
}