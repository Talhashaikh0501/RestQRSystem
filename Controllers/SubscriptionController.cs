using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionController(ApplicationDbContext context)
        {
            _context = context;
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
            // IMPORTANT:
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
            // CREATE RESTAURANT ONLY AFTER PAYMENT SUCCESS
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
            // CREATE ACTIVE SUBSCRIPTION
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
            // PAYMENT SUCCESS
            // =====================================================

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

            return View(subscription);
        }
    }
}