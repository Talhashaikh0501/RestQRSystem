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
            return HttpContext.Session.GetObject<CartViewModel>(CartKey);
        }

        private void SaveCart(CartViewModel cart)
        {
            HttpContext.Session.SetObject(CartKey, cart);
        }

        // ---------------------------------------------
        // CART PAGE
        // ---------------------------------------------
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart ?? new CartViewModel());
        }

        // ---------------------------------------------
        // ADD ITEM (Updated for JS/UI Sync)
        // ---------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int menuItemId, string qrToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken)) return BadRequest();

            var table = await _context.RestaurantTables
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.QRToken == qrToken && t.IsActive && t.Restaurant.IsActive);

            if (table == null) return NotFound();

            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == menuItemId && m.IsAvailable && m.Category.IsActive && m.Category.RestaurantId == table.RestaurantId);

            if (menuItem == null) return NotFound();

            var cart = GetCart();

            if (cart == null || cart.RestaurantId != table.RestaurantId || cart.TableId != table.Id)
            {
                cart = new CartViewModel
                {
                    RestaurantId = table.RestaurantId,
                    TableId = table.Id,
                    TableNumber = table.TableNumber,
                    QRToken = table.QRToken
                };
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItem.Id);

            if (existingItem == null)
            {
                cart.Items.Add(new CartItemViewModel
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Price = menuItem.Price,
                    Quantity = 1,
                    ImageUrl = menuItem.ImageUrl
                });
            }
            else
            {
                existingItem.Quantity++;
            }

            SaveCart(cart);

            // AJAX Response matching JS expectations
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    totalQuantity = cart.TotalQuantity,
                    totalPrice = cart.Subtotal // This matches the JS variable 'result.totalPrice'
                });
            }

            return RedirectToAction("Scan", "Menu", new { id = qrToken });
        }

        // ---------------------------------------------
        // QUANTITY ACTIONS
        // ---------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Increase(int menuItemId)
        {
            var cart = GetCart();
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (item != null)
                {
                    item.Quantity++;
                    SaveCart(cart);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int menuItemId)
        {
            var cart = GetCart();
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (item != null)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0) cart.Items.Remove(item);
                    SaveCart(cart);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int menuItemId)
        {
            var cart = GetCart();
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
                if (item != null)
                {
                    cart.Items.Remove(item);
                    SaveCart(cart);
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}