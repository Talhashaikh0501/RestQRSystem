using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Helpers;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    [AllowAnonymous]
    public class MenuController : Controller
    {
        private const string CustomerDetailsKey =
            "RestaurantQR_CustomerDetails";

        private const string CartKey =
            "RestaurantQR_Cart";

        private readonly ApplicationDbContext _context;

        public MenuController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // SCAN QR CODE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Scan(
            string id)
        {
            // -------------------------------------------------
            // CHECK QR TOKEN
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // -------------------------------------------------
            // FIND TABLE
            // -------------------------------------------------

            var table =
                await _context.RestaurantTables
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
            // CUSTOMER MUST ENTER DETAILS FIRST
            // =================================================

            var customer =
                HttpContext.Session
                    .GetObject<CustomerDetailsViewModel>(
                        CustomerDetailsKey);

            var customerIsValid =
                customer != null &&
                customer.RestaurantId ==
                    table.RestaurantId &&
                customer.TableId ==
                    table.Id &&
                !string.IsNullOrWhiteSpace(
                    customer.CustomerName) &&
                !string.IsNullOrWhiteSpace(
                    customer.CustomerPhone);

            if (!customerIsValid)
            {
                return RedirectToAction(
                    nameof(CustomerDetails),
                    new
                    {
                        id = table.QRToken
                    });
            }

            // =================================================
            // LOAD RESTAURANT MENU
            // =================================================

            var categories =
                await _context.Categories

                    .Where(c =>
                        c.RestaurantId ==
                            table.RestaurantId &&
                        c.IsActive)

                    .OrderBy(c =>
                        c.DisplayOrder)

                    .Select(c =>
                        new QRMenuCategoryViewModel
                        {
                            Name =
                                c.Name,

                            MenuItems =
                                c.MenuItems

                                    .Where(m =>
                                        m.IsAvailable)

                                    .OrderBy(m =>
                                        m.Name)

                                    .Select(m =>
                                        new QRMenuItemViewModel
                                        {
                                            Id =
                                                m.Id,

                                            Name =
                                                m.Name,

                                            Description =
                                                m.Description,

                                            Price =
                                                m.Price,

                                            ImageUrl =
                                                m.ImageUrl,

                                            Options =
                                                m.Options

                                                    .Where(o =>
                                                        o.IsAvailable)

                                                    .OrderBy(o =>
                                                        o.DisplayOrder)

                                                    .Select(o =>
                                                        new QRMenuItemOptionViewModel
                                                        {
                                                            Id =
                                                                o.Id,

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
                                        })

                                    .ToList()
                        })

                    .ToListAsync();

            // =================================================
            // REMOVE EMPTY CATEGORIES
            // =================================================

            categories =
                categories
                    .Where(c =>
                        c.MenuItems.Count > 0)
                    .ToList();

            // =================================================
            // CURRENT CART
            // =================================================

            var cart =
                HttpContext.Session
                    .GetObject<CartViewModel>(
                        CartKey);

            var cartBelongsToCurrentTable =
                cart != null &&
                cart.RestaurantId ==
                    table.RestaurantId &&
                cart.TableId ==
                    table.Id;

            // =================================================
            // CREATE MENU MODEL
            // =================================================

            var model =
                new QRMenuViewModel
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

                    CustomerName =
                        customer!.CustomerName,

                    CartQuantity =
                        cartBelongsToCurrentTable
                            ? cart!.TotalQuantity
                            : 0,

                    CartTotal =
                        cartBelongsToCurrentTable
                            ? cart!.Subtotal
                            : 0,

                    Categories =
                        categories
                };

            // =================================================
            // OPEN MENU
            // =================================================

            return View(
                "~/Views/Menu/Index.cshtml",
                model);
        }

        // =====================================================
        // CUSTOMER DETAILS PAGE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(
            string id,
            bool change = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // -------------------------------------------------
            // VALIDATE QR / TABLE
            // -------------------------------------------------

            var table =
                await _context.RestaurantTables
                    .Include(t => t.Restaurant)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.QRToken == id &&
                        t.IsActive &&
                        t.Restaurant.IsActive);

            if (table == null)
            {
                return NotFound();
            }

            // -------------------------------------------------
            // CHECK EXISTING CUSTOMER
            // -------------------------------------------------

            var existing =
                HttpContext.Session
                    .GetObject<CustomerDetailsViewModel>(
                        CustomerDetailsKey);

            // Same customer/session + same table:
            // do not ask them again.
            if (!change &&
                existing != null &&
                existing.RestaurantId ==
                    table.RestaurantId &&
                existing.TableId ==
                    table.Id &&
                !string.IsNullOrWhiteSpace(
                    existing.CustomerName) &&
                !string.IsNullOrWhiteSpace(
                    existing.CustomerPhone))
            {
                return RedirectToAction(
                    nameof(Scan),
                    new
                    {
                        id = table.QRToken
                    });
            }

            // -------------------------------------------------
            // CREATE FORM MODEL
            // -------------------------------------------------

            var model =
                new CustomerDetailsViewModel
                {
                    RestaurantId =
                        table.RestaurantId,

                    TableId =
                        table.Id,

                    RestaurantName =
                        table.Restaurant.Name,

                    TableNumber =
                        table.TableNumber,

                    QRToken =
                        table.QRToken,

                    CustomerName =
                        existing != null &&
                        existing.RestaurantId ==
                            table.RestaurantId
                            ? existing.CustomerName
                            : string.Empty,

                    CustomerPhone =
                        existing != null &&
                        existing.RestaurantId ==
                            table.RestaurantId
                            ? existing.CustomerPhone
                            : string.Empty
                };

            return View(
                "~/Views/Menu/CustomerDetails.cshtml",
                model);
        }

        // =====================================================
        // SAVE CUSTOMER DETAILS
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerDetails(
            CustomerDetailsViewModel model)
        {
            // -------------------------------------------------
            // NEVER TRUST HIDDEN RESTAURANT/TABLE VALUES
            // VALIDATE AGAIN USING QR TOKEN
            // -------------------------------------------------

            var table =
                await _context.RestaurantTables
                    .Include(t => t.Restaurant)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.QRToken ==
                            model.QRToken &&
                        t.IsActive &&
                        t.Restaurant.IsActive);

            if (table == null)
            {
                return NotFound();
            }

            // -------------------------------------------------
            // RESTORE DISPLAY DATA
            // -------------------------------------------------

            model.RestaurantId =
                table.RestaurantId;

            model.TableId =
                table.Id;

            model.RestaurantName =
                table.Restaurant.Name;

            model.TableNumber =
                table.TableNumber;

            // -------------------------------------------------
            // NORMALIZE PHONE
            // -------------------------------------------------

            var normalizedPhone =
                NormalizePhone(
                    model.CustomerPhone);

            if (normalizedPhone == null)
            {
                ModelState.AddModelError(
                    nameof(
                        model.CustomerPhone),
                    "Enter a valid phone number with 7 to 15 digits.");
            }

            // -------------------------------------------------
            // VALIDATION FAILED
            // -------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Menu/CustomerDetails.cshtml",
                    model);
            }

            // -------------------------------------------------
            // CLEAN CUSTOMER DETAILS
            // -------------------------------------------------

            model.CustomerName =
                model.CustomerName.Trim();

            model.CustomerPhone =
                normalizedPhone!;

            // =================================================
            // IF QR CHANGED TO ANOTHER TABLE,
            // REMOVE OLD TABLE CART
            // =================================================

            var existingCart =
                HttpContext.Session
                    .GetObject<CartViewModel>(
                        CartKey);

            if (existingCart != null &&
                (
                    existingCart.RestaurantId !=
                        table.RestaurantId ||

                    existingCart.TableId !=
                        table.Id
                ))
            {
                HttpContext.Session
                    .Remove(CartKey);
            }

            // =================================================
            // SAVE CUSTOMER FOR CURRENT SESSION
            // =================================================

            HttpContext.Session
                .SetObject(
                    CustomerDetailsKey,
                    model);

            // =================================================
            // NOW OPEN THE MENU
            // =================================================

            return RedirectToAction(
                nameof(Scan),
                new
                {
                    id = table.QRToken
                });
        }

        // =====================================================
        // PHONE NORMALIZATION
        // =====================================================

        private static string? NormalizePhone(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed =
                value.Trim();

            var hasLeadingPlus =
                trimmed.StartsWith('+');

            var digits =
                new string(
                    trimmed
                        .Where(char.IsDigit)
                        .ToArray());

            // International phone numbers are generally
            // between 7 and 15 digits.
            if (digits.Length < 7 ||
                digits.Length > 15)
            {
                return null;
            }

            return hasLeadingPlus
                ? $"+{digits}"
                : digits;
        }
    }
}