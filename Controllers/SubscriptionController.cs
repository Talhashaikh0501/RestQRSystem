using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;
using System.Security.Cryptography;

namespace RestaurantQR.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // GET: /Subscription/Buy?planId=1
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Buy(int planId)
        {
            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p =>
                    p.Id == planId &&
                    p.IsActive);

            if (plan == null)
            {
                return NotFound();
            }

            var model = new PurchaseSubscriptionViewModel
            {
                SubscriptionPlanId = plan.Id,
                PlanName = plan.Name,
                Amount = plan.Price,
                DurationDays = plan.DurationDays,
                StartDate = DateTime.UtcNow.Date
            };

            return View(model);
        }


        // =========================================================
        // POST: /Subscription/Buy
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(
            PurchaseSubscriptionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p =>
                    p.Id == model.SubscriptionPlanId &&
                    p.IsActive);

            if (plan == null)
            {
                return NotFound();
            }

            // Always take pricing information
            // from database instead of trusting hidden fields.
            model.PlanName = plan.Name;
            model.Amount = plan.Price;
            model.DurationDays = plan.DurationDays;

            return View("PaymentCheckout", model);
        }


        // =========================================================
        // POST: /Subscription/PaymentCheckout
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentCheckout(
            PurchaseSubscriptionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p =>
                    p.Id == model.SubscriptionPlanId &&
                    p.IsActive);

            if (plan == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Payment is currently simulated.
            // Razorpay will be integrated later.
            // -----------------------------------------------------

            if (model.PaymentMethod == null)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethod),
                    "Please select a payment method.");

                return View(model);
            }

            // =====================================================
            // CREATE RESTAURANT
            // =====================================================

            var restaurant = new Restaurant
            {
                Name = model.RestaurantName,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);

            await _context.SaveChangesAsync();

            // =====================================================
            // CREATE SUBSCRIPTION
            // =====================================================

            var subscription = new Subscription
            {
                RestaurantId = restaurant.Id,
                SubscriptionPlanId = plan.Id,

                StartDate = model.StartDate,

                EndDate = model.StartDate
                    .AddDays(plan.DurationDays),

                Amount = plan.Price,

                Status = SubscriptionStatus.Active,

                PaymentStatus = PaymentStatus.Paid,

                PaymentMethod = model.PaymentMethod,

                PaidAt = DateTime.UtcNow,

                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);

            await _context.SaveChangesAsync();

            // =====================================================
            // CREATE RESTAURANT ADMIN ACCOUNT
            // =====================================================

            var adminUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.OwnerName,
                PhoneNumber = model.Phone,
                EmailConfirmed = true,
                RestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Temporary password for the new restaurant admin.
            var temporaryPassword = GenerateTemporaryPassword();
            var userResult = await _userManager.CreateAsync(
                adminUser,
                temporaryPassword);

            if (!userResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    userResult.Errors.Select(e => e.Description));

                ModelState.AddModelError(
                    string.Empty,
                    $"Admin account could not be created: {errors}");

                return View(model);
            }

            // =====================================================
            // ASSIGN RESTAURANT ADMIN ROLE
            // =====================================================

            var roleResult = await _userManager.AddToRoleAsync(
                adminUser,
                "RestaurantAdmin");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(e => e.Description));

                ModelState.AddModelError(
                    string.Empty,
                    $"Restaurant Admin role could not be assigned: {errors}");

                return View(model);
            }

            // =====================================================
            // PAYMENT SUCCESS
            // =====================================================

            TempData["AdminEmail"] = adminUser.Email;
            TempData["TemporaryPassword"] = temporaryPassword;

            return RedirectToAction(
                nameof(Success),
                new
                {
                    subscriptionId = subscription.Id
                });
        }

        // =========================================================
        // GET: /Subscription/PaymentCheckout
        // =========================================================
        [HttpGet]
        public IActionResult PaymentCheckout()
        {
            return RedirectToAction(
                nameof(Buy));
        }


        // =========================================================
        // GET: /Subscription/Success
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Success(int subscriptionId)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.Restaurant)
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s =>
                    s.Id == subscriptionId);

            if (subscription == null)
            {
                return NotFound();
            }

            var model = new SubscriptionSuccessViewModel
            {
                Subscription = subscription,
                AdminEmail = TempData["AdminEmail"]?.ToString() ?? string.Empty,
                TemporaryPassword = TempData["TemporaryPassword"]?.ToString() ?? string.Empty
            };

            return View(model);
        }
        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";

            var random = RandomNumberGenerator.Create();

            string GetRandomChar(string chars)
            {
                var bytes = new byte[4];
                random.GetBytes(bytes);

                var index = BitConverter.ToUInt32(bytes, 0) % chars.Length;
                return chars[(int)index].ToString();
            }

            return
                GetRandomChar(upper) +
                GetRandomChar(lower) +
                GetRandomChar(digits) +
                GetRandomChar(upper + lower + digits) +
                GetRandomChar(upper + lower + digits) +
                GetRandomChar(upper + lower + digits) +
                GetRandomChar(upper + lower + digits) +
                GetRandomChar(upper + lower + digits);
        }
    }
}