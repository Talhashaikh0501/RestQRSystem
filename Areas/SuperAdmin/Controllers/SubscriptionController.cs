using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Hubs;
using RestaurantQR.Models;
using RestaurantQR.Services;

namespace RestaurantQR.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly IEmailService _emailService;
        private readonly ILogger<SubscriptionController> _logger;


        public SubscriptionController(
            ApplicationDbContext context,
            IHubContext<OrderHub> orderHub,
            IEmailService emailService,
            ILogger<SubscriptionController> logger)
        {
            _context = context;
            _orderHub = orderHub;
            _emailService = emailService;
            _logger = logger;
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
        // APPROVE PAID SUBSCRIPTION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSubscription(
            int id)
        {
            var subscription =
                await _context.Subscriptions

                    .Include(s => s.Restaurant)

                    .Include(s => s.SubscriptionPlan)

                    .FirstOrDefaultAsync(
                        s => s.Id == id
                    );


            if (subscription == null)
            {
                return NotFound();
            }


            // =====================================================
            // PAYMENT MUST BE COMPLETED
            // =====================================================

            if (subscription.PaymentStatus !=
                PaymentStatus.Paid)
            {
                TempData["Error"] =
                    "This subscription cannot be approved because its payment has not been completed.";


                return RedirectToAction(
                    nameof(Subscriptions)
                );
            }


            // =====================================================
            // ALREADY ACTIVE
            // =====================================================

            if (subscription.Status ==
                SubscriptionStatus.Active)
            {
                TempData["Info"] =
                    "This subscription is already active.";


                return RedirectToAction(
                    nameof(Subscriptions)
                );
            }


            // =====================================================
            // RESTAURANT MUST EXIST
            // =====================================================

            if (subscription.Restaurant == null)
            {
                TempData["Error"] =
                    "Restaurant information could not be found for this subscription.";


                return RedirectToAction(
                    nameof(Subscriptions)
                );
            }


            // =====================================================
            // FIND RESTAURANT ADMIN
            // =====================================================

            var adminUser =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.RestaurantId ==
                                subscription.RestaurantId
                            &&
                            u.Email ==
                                subscription.Restaurant.Email
                    );


            // Fallback:
            // If restaurant email and Identity email somehow differ,
            // find the first user belonging to this restaurant.

            adminUser ??=
                await _context.Users
                    .Where(
                        u =>
                            u.RestaurantId ==
                            subscription.RestaurantId
                    )
                    .OrderBy(
                        u => u.CreatedAt
                    )
                    .FirstOrDefaultAsync();


            if (adminUser == null)
            {
                TempData["Error"] =
                    "Restaurant Admin account could not be found. The subscription was not approved.";


                return RedirectToAction(
                    nameof(Subscriptions)
                );
            }


            // =====================================================
            // ACTIVATE SUBSCRIPTION
            // =====================================================
            //
            // Subscription time begins from the date SuperAdmin
            // approves the account.
            //
            // This prevents the restaurant from losing subscription
            // days while waiting for approval.
            // =====================================================

            var approvalDate =
                DateTime.UtcNow.Date;


            subscription.Status =
                SubscriptionStatus.Active;


            subscription.StartDate =
                approvalDate;


            subscription.EndDate =
                approvalDate.AddDays(
                    subscription
                        .SubscriptionPlan
                        .DurationDays
                );


            subscription.UpdatedAt =
                DateTime.UtcNow;


            // =====================================================
            // ACTIVATE RESTAURANT
            // =====================================================

            subscription.Restaurant.IsActive =
                true;


            try
            {
                await _context
                    .SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SuperAdmin failed to approve subscription {SubscriptionId}.",
                    subscription.Id
                );


                TempData["Error"] =
                    "Something went wrong while approving the subscription.";


                return RedirectToAction(
                    nameof(Subscriptions)
                );
            }


            // =====================================================
            // SEND APPROVAL EMAIL
            // =====================================================
            //
            // IMPORTANT:
            //
            // Approval has already been saved.
            // If email sending fails, the restaurant stays Active.
            // =====================================================

            try
            {
                var recipientEmail =
                    adminUser.Email;


                if (string.IsNullOrWhiteSpace(
                    recipientEmail))
                {
                    recipientEmail =
                        subscription.Restaurant.Email;
                }


                if (!string.IsNullOrWhiteSpace(
                    recipientEmail))
                {
                    await _emailService
                        .SendSubscriptionApprovedEmailAsync(
                            recipientEmail:
                                recipientEmail,

                            ownerName:
                                adminUser.FullName
                                ?? "Restaurant Administrator",

                            restaurantName:
                                subscription.Restaurant.Name,

                            planName:
                                subscription.SubscriptionPlan.Name,

                            adminEmail:
                                adminUser.Email
                                ?? recipientEmail
                        );
                }


                TempData["Success"] =
                    $"Subscription for {subscription.Restaurant.Name} has been approved successfully. The Restaurant Admin has been notified by email.";
            }
            catch (Exception ex)
            {
                // =================================================
                // APPROVAL MUST NOT BE UNDONE IF EMAIL FAILS
                // =================================================

                _logger.LogError(
                    ex,
                    "Subscription {SubscriptionId} was approved, but approval email could not be sent.",
                    subscription.Id
                );


                TempData["Success"] =
                    $"Subscription for {subscription.Restaurant.Name} has been approved successfully.";


                TempData["Warning"] =
                    "The account is active, but the approval email could not be sent.";
            }


            // =====================================================
            // LIVE SUPERADMIN ANALYTICS UPDATE
            // =====================================================

            await NotifySuperAdminAnalyticsChangedAsync(
                source:
                    "SubscriptionApproved",

                planId:
                    subscription.SubscriptionPlanId,

                restaurantId:
                    subscription.RestaurantId,

                subscriptionId:
                    subscription.Id
            );


            return RedirectToAction(
                nameof(Subscriptions)
            );
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
                int? planId = null,
                int? restaurantId = null,
                int? subscriptionId = null)
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

                            restaurantId,

                            subscriptionId,

                            occurredAtUtc =
                                DateTime.UtcNow
                        }
                    );
            }
            catch
            {
                // SignalR is only responsible for instant UI
                // refresh.
                //
                // Notification failure must never undo a
                // successful database operation.
            }
        }
    }
}