using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.Services;
using RestaurantQR.ViewModels;
using System.Security.Cryptography;

namespace RestaurantQR.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("RestaurantAdmin");

            var result = new List<RestaurantAdminListViewModel>();

            foreach (var user in adminUsers)
            {
                var restaurant = await _context.Restaurants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == user.RestaurantId);

                result.Add(new RestaurantAdminListViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName ?? "Unknown",
                    Email = user.Email ?? "",
                    RestaurantId = user.RestaurantId ?? 0,
                    RestaurantName = restaurant?.Name ?? "Unassigned",
                    RestaurantIsActive = restaurant?.IsActive ?? false
                });
            }

            return View(
                "~/Areas/SuperAdmin/Views/Admin/Index.cshtml",
                result);
        }

        // =========================================================
        // ACTIVATE RESTAURANT ADMIN / APPROVE SUBSCRIPTION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            if (!admin.RestaurantId.HasValue)
            {
                TempData["ErrorMessage"] =
                    "This Restaurant Admin is not assigned to a restaurant.";

                return RedirectToAction(nameof(Index));
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == admin.RestaurantId.Value);

            if (restaurant == null)
            {
                TempData["ErrorMessage"] =
                    "Restaurant information could not be found for this Admin.";

                return RedirectToAction(nameof(Index));
            }

            if (restaurant.IsActive)
            {
                TempData["SuccessMessage"] =
                    $"{restaurant.Name} is already active.";

                return RedirectToAction(nameof(Index));
            }

            // If this restaurant was manually deactivated earlier and still
            // has a valid paid Active subscription, simply reactivate it.
            var today = DateTime.UtcNow.Date;

            var activeSubscription = await _context.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.RestaurantId == restaurant.Id &&
                    s.Status == SubscriptionStatus.Active &&
                    s.PaymentStatus == PaymentStatus.Paid &&
                    s.EndDate >= today)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeSubscription != null)
            {
                restaurant.IsActive = true;

                try
                {
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        $"{restaurant.Name} has been reactivated successfully.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to reactivate Restaurant Admin {AdminId} for restaurant {RestaurantId}.",
                        admin.Id,
                        restaurant.Id);

                    TempData["ErrorMessage"] =
                        "Something went wrong while reactivating the restaurant.";
                }

                return RedirectToAction(nameof(Index));
            }

            // Otherwise this is the first approval of a paid pending request.
            var pendingSubscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s =>
                    s.RestaurantId == restaurant.Id &&
                    s.Status == SubscriptionStatus.Pending)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (pendingSubscription == null)
            {
                TempData["ErrorMessage"] =
                    "No valid active subscription or pending subscription request was found for this restaurant.";

                return RedirectToAction(nameof(Index));
            }

            if (pendingSubscription.PaymentStatus != PaymentStatus.Paid)
            {
                TempData["ErrorMessage"] =
                    "This restaurant cannot be activated because payment has not been completed.";

                return RedirectToAction(nameof(Index));
            }

            var approvalDate = DateTime.UtcNow.Date;

            pendingSubscription.Status = SubscriptionStatus.Active;
            pendingSubscription.StartDate = approvalDate;
            pendingSubscription.EndDate = approvalDate.AddDays(
                pendingSubscription.SubscriptionPlan.DurationDays);
            pendingSubscription.UpdatedAt = DateTime.UtcNow;

            restaurant.IsActive = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to activate Restaurant Admin {AdminId} for restaurant {RestaurantId}.",
                    admin.Id,
                    restaurant.Id);

                TempData["ErrorMessage"] =
                    "Something went wrong while activating the restaurant.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(admin.Email))
                {
                    await _emailService.SendSubscriptionApprovedEmailAsync(
                        recipientEmail: admin.Email,
                        ownerName: admin.FullName ?? "Restaurant Administrator",
                        restaurantName: restaurant.Name,
                        planName: pendingSubscription.SubscriptionPlan.Name,
                        adminEmail: admin.Email);
                }

                TempData["SuccessMessage"] =
                    $"{restaurant.Name} has been activated successfully. Approval email sent to the Admin.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Restaurant {RestaurantId} was activated, but approval email failed for Admin {AdminId}.",
                    restaurant.Id,
                    admin.Id);

                TempData["SuccessMessage"] =
                    $"{restaurant.Name} has been activated successfully.";

                TempData["WarningMessage"] =
                    "The account is active, but the approval email could not be sent.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DEACTIVATE RESTAURANT ADMIN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            if (!admin.RestaurantId.HasValue)
            {
                TempData["ErrorMessage"] =
                    "This Restaurant Admin is not assigned to a restaurant.";

                return RedirectToAction(nameof(Index));
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == admin.RestaurantId.Value);

            if (restaurant == null)
            {
                TempData["ErrorMessage"] =
                    "Restaurant information could not be found for this Admin.";

                return RedirectToAction(nameof(Index));
            }

            if (!restaurant.IsActive)
            {
                TempData["SuccessMessage"] =
                    $"{restaurant.Name} is already inactive.";

                return RedirectToAction(nameof(Index));
            }

            restaurant.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to deactivate Restaurant Admin {AdminId} for restaurant {RestaurantId}.",
                    admin.Id,
                    restaurant.Id);

                TempData["ErrorMessage"] =
                    "Something went wrong while deactivating the restaurant.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // SEND DEACTIVATION EMAIL AFTER SUCCESSFUL DEACTIVATION
            // =====================================================
            // Deactivation is already saved. If SMTP/email fails,
            // the account remains inactive and SuperAdmin gets a
            // warning instead of the whole action failing.
            // =====================================================
            try
            {
                if (!string.IsNullOrWhiteSpace(admin.Email))
                {
                    await _emailService.SendSubscriptionDeactivatedEmailAsync(
                        recipientEmail: admin.Email,
                        ownerName: admin.FullName ?? "Restaurant Administrator",
                        restaurantName: restaurant.Name);

                    TempData["SuccessMessage"] =
                        $"{restaurant.Name} has been deactivated successfully. Deactivation email sent to the Admin.";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        $"{restaurant.Name} has been deactivated successfully.";

                    TempData["WarningMessage"] =
                        "The account was deactivated, but the Restaurant Admin does not have an email address.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Restaurant {RestaurantId} was deactivated, but deactivation email failed for Admin {AdminId}.",
                    restaurant.Id,
                    admin.Id);

                TempData["SuccessMessage"] =
                    $"{restaurant.Name} has been deactivated successfully.";

                TempData["WarningMessage"] =
                    "The account is inactive, but the deactivation email could not be sent.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            var model = new ResetAdminPasswordViewModel
            {
                UserId = admin.Id,
                AdminEmail = admin.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetAdminPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _userManager.FindByIdAsync(model.UserId);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(admin);

            var result =
                await _userManager.ResetPasswordAsync(
                    admin,
                    token,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Admin password changed successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ResetPasswordSuccess(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AdminEmail = email;
            ViewBag.TemporaryPassword = password;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            var result = await _userManager.DeleteAsync(admin);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    string.Join(
                        " ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Restaurant Admin deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string all = upper + lower + digits;

            using var random = RandomNumberGenerator.Create();

            string GetRandomChar(string chars)
            {
                var bytes = new byte[4];
                random.GetBytes(bytes);

                var index =
                    BitConverter.ToUInt32(bytes, 0) %
                    (uint)chars.Length;

                return chars[(int)index].ToString();
            }

            return
                GetRandomChar(upper) +
                GetRandomChar(lower) +
                GetRandomChar(digits) +
                GetRandomChar(all) +
                GetRandomChar(all) +
                GetRandomChar(all) +
                GetRandomChar(all) +
                GetRandomChar(all);
        }
    }
}
