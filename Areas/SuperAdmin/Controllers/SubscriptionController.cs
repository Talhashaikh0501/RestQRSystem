using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Hubs;
using RestaurantQR.Models;

namespace RestaurantQR.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _orderHub;


        public SubscriptionController(
            ApplicationDbContext context,
            IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _orderHub = orderHub;
        }


        // =========================================================
        // SUBSCRIPTION PLANS
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var plans =
                await _context.SubscriptionPlans
                    .OrderBy(p => p.DurationDays)
                    .ToListAsync();


            return View(plans);
        }


        // =========================================================
        // ALL RESTAURANT SUBSCRIPTIONS
        // =========================================================

        public async Task<IActionResult> Subscriptions()
        {
            var subscriptions =
                await _context.Subscriptions

                    .Include(s => s.Restaurant)

                    .Include(s => s.SubscriptionPlan)

                    .OrderByDescending(
                        s => s.CreatedAt
                    )

                    .ToListAsync();


            return View(subscriptions);
        }


        // =========================================================
        // CREATE SUBSCRIPTION PLAN
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SubscriptionPlan plan)
        {
            if (!ModelState.IsValid)
            {
                return View(plan);
            }


            plan.CreatedAt =
                DateTime.UtcNow;


            plan.UpdatedAt =
                null;


            _context.SubscriptionPlans.Add(
                plan
            );


            await _context
                .SaveChangesAsync();


            // =====================================================
            // LIVE SUPERADMIN ANALYTICS UPDATE
            // =====================================================

            await NotifySuperAdminAnalyticsChangedAsync(
                source:
                    "SubscriptionPlanCreated",

                planId:
                    plan.Id
            );


            TempData["Success"] =
                "Subscription plan created successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // EDIT SUBSCRIPTION PLAN
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var plan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (plan == null)
            {
                return NotFound();
            }


            return View(plan);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            SubscriptionPlan plan)
        {
            if (!ModelState.IsValid)
            {
                return View(plan);
            }


            var existingPlan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(
                        p => p.Id == plan.Id
                    );


            if (existingPlan == null)
            {
                return NotFound();
            }


            // =====================================================
            // UPDATE EXISTING PLAN
            // =====================================================

            existingPlan.Name =
                plan.Name;


            existingPlan.DurationDays =
                plan.DurationDays;


            existingPlan.Price =
                plan.Price;


            existingPlan.IsCustom =
                plan.IsCustom;


            existingPlan.IsActive =
                plan.IsActive;


            existingPlan.UpdatedAt =
                DateTime.UtcNow;


            await _context
                .SaveChangesAsync();


            // =====================================================
            // LIVE SUPERADMIN ANALYTICS UPDATE
            // =====================================================
            //
            // Example:
            //
            // "Premium" renamed to "Business Pro"
            //
            // Existing active subscriptions on that plan will
            // immediately receive the new graph label.
            // =====================================================

            await NotifySuperAdminAnalyticsChangedAsync(
                source:
                    "SubscriptionPlanUpdated",

                planId:
                    existingPlan.Id
            );


            TempData["Success"] =
                "Subscription plan updated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // ACTIVATE / DEACTIVATE PLAN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id)
        {
            var plan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );


            if (plan == null)
            {
                return NotFound();
            }


            plan.IsActive =
                !plan.IsActive;


            plan.UpdatedAt =
                DateTime.UtcNow;


            await _context
                .SaveChangesAsync();


            // =====================================================
            // LIVE SUPERADMIN ANALYTICS UPDATE
            // =====================================================

            await NotifySuperAdminAnalyticsChangedAsync(
                source:
                    plan.IsActive
                        ? "SubscriptionPlanActivated"
                        : "SubscriptionPlanDeactivated",

                planId:
                    plan.Id
            );


            TempData["Success"] =
                plan.IsActive
                    ? "Subscription plan activated successfully."
                    : "Subscription plan deactivated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // SUPERADMIN ANALYTICS NOTIFICATION
        // =========================================================

        private async Task
            NotifySuperAdminAnalyticsChangedAsync(
                string source,
                int? planId = null)
        {
            try
            {
                await _orderHub.Clients

                    .Group(
                        OrderHub
                            .GetSuperAdminAnalyticsGroup()
                    )

                    .SendAsync(
                        "PlatformAnalyticsChanged",
                        new
                        {
                            source,

                            planId,

                            occurredAtUtc =
                                DateTime.UtcNow
                        }
                    );
            }
            catch
            {
                // SignalR is only responsible for instant UI
                // refresh. A notification failure must never
                // undo a successful database operation.
            }
        }
    }
}