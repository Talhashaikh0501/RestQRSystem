using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.Services;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "RestaurantAdmin")]
    public class TableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QRCodeService _qrCodeService;
        private readonly IConfiguration _configuration;

        public TableController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            QRCodeService qrCodeService,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _qrCodeService = qrCodeService;
            _configuration = configuration;
        }

        private async Task<int?> GetRestaurantIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.RestaurantId;
        }
        private string GetMenuUrl(string qrToken)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/Menu/Scan/{qrToken}";
        }

        // -------------------------------------------------
        // LIST
        // -------------------------------------------------

        public async Task<IActionResult> Index()
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var tables = await _context.RestaurantTables
                .Where(t =>
                    t.RestaurantId == restaurantId)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            return View(
                "~/Areas/Admin/Views/Table/Index.cshtml",
                tables);
        }

        // -------------------------------------------------
        // CREATE
        // -------------------------------------------------

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                "~/Areas/Admin/Views/Table/Create.cshtml",
                new RestaurantTableViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            RestaurantTableViewModel model)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Areas/Admin/Views/Table/Create.cshtml",
                    model);
            }

            var tableNumber =
                model.TableNumber.Trim();

            var alreadyExists =
                await _context.RestaurantTables
                    .AnyAsync(t =>
                        t.RestaurantId == restaurantId &&
                        t.TableNumber == tableNumber);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    nameof(model.TableNumber),
                    "This table number already exists.");

                return View(
                    "~/Areas/Admin/Views/Table/Create.cshtml",
                    model);
            }

            var table = new RestaurantTable
            {
                TableNumber = tableNumber,
                Capacity = model.Capacity,
                IsActive = model.IsActive,
                RestaurantId = restaurantId.Value,

                QRToken =
                    Guid.NewGuid().ToString("N")
            };

            _context.RestaurantTables.Add(table);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Table created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------
        // EDIT
        // -------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var table =
                await _context.RestaurantTables
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.RestaurantId == restaurantId);

            if (table == null)
            {
                return NotFound();
            }

            var model =
                new RestaurantTableViewModel
                {
                    Id = table.Id,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    IsActive = table.IsActive
                };

            return View(
                "~/Areas/Admin/Views/Table/Edit.cshtml",
                model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            RestaurantTableViewModel model)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Areas/Admin/Views/Table/Edit.cshtml",
                    model);
            }

            var table =
                await _context.RestaurantTables
                    .FirstOrDefaultAsync(t =>
                        t.Id == model.Id &&
                        t.RestaurantId == restaurantId);

            if (table == null)
            {
                return NotFound();
            }

            var tableNumber =
                model.TableNumber.Trim();

            var duplicate =
                await _context.RestaurantTables
                    .AnyAsync(t =>
                        t.RestaurantId == restaurantId &&
                        t.TableNumber == tableNumber &&
                        t.Id != model.Id);

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(model.TableNumber),
                    "This table number already exists.");

                return View(
                    "~/Areas/Admin/Views/Table/Edit.cshtml",
                    model);
            }

            table.TableNumber = tableNumber;
            table.Capacity = model.Capacity;
            table.IsActive = model.IsActive;

            // QRToken is intentionally preserved.
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Table updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------
        // VIEW QR
        // -------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> QRCode(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var table =
                await _context.RestaurantTables
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.RestaurantId == restaurantId);

            if (table == null)
            {
                return NotFound();
            }

            var menuUrl =
                GetMenuUrl(table.QRToken);

            if (string.IsNullOrWhiteSpace(menuUrl))
            {
                return BadRequest(
                    "PublicBaseUrl is not configured.");
            }

            var qrBytes =
                _qrCodeService.GenerateQRCode(menuUrl);

            return File(
                qrBytes,
                "image/png");
        }

        // -------------------------------------------------
        // DOWNLOAD QR
        // -------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> DownloadQR(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var table =
                await _context.RestaurantTables
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.RestaurantId == restaurantId);

            if (table == null)
            {
                return NotFound();
            }

            var menuUrl =
                GetMenuUrl(table.QRToken);

            if (string.IsNullOrWhiteSpace(menuUrl))
            {
                return BadRequest(
                    "PublicBaseUrl is not configured.");
            }

            var qrBytes =
                _qrCodeService.GenerateQRCode(menuUrl);

            var safeTableNumber =
                string.Concat(
                    table.TableNumber.Where(c =>
                        char.IsLetterOrDigit(c) ||
                        c == '-' ||
                        c == '_'));

            if (string.IsNullOrWhiteSpace(
                safeTableNumber))
            {
                safeTableNumber =
                    table.Id.ToString();
            }

            return File(
                qrBytes,
                "image/png",
                $"RestaurantQR-{safeTableNumber}.png");
        }

        // -------------------------------------------------
        // DELETE
        // -------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurantId = await GetRestaurantIdAsync();

            if (restaurantId == null)
            {
                return Forbid();
            }

            var table =
                await _context.RestaurantTables
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.RestaurantId == restaurantId);

            if (table == null)
            {
                return NotFound();
            }

            var hasOrderHistory =
                await _context.Orders
                    .AnyAsync(o =>
                        o.RestaurantTableId == table.Id &&
                        o.RestaurantId == restaurantId);

            if (hasOrderHistory)
            {
                TempData["Error"] =
                    "This table has order history and cannot be deleted. Mark it inactive instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.RestaurantTables.Remove(table);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Table deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}