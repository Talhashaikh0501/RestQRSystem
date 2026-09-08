using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Hubs;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class RestaurantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<OrderHub> _orderHub;


        public RestaurantController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _userManager = userManager;
            _orderHub = orderHub;
        }


        // =========================================================
        // GET: SuperAdmin/Restaurant/Index
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var restaurants =
                await _context.Restaurants
                    .ToListAsync();


            return View(restaurants);
        }


        // =========================================================
        // GET: SuperAdmin/Restaurant/Create
        // =========================================================

        public IActionResult Create()
        {
            return View();
        }


        // =========================================================
        // POST: SuperAdmin/Restaurant/Create
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateRestaurantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // =====================================================
            // DATABASE TRANSACTION
            // =====================================================
            //
            // Restaurant and RestaurantAdmin should either both
            // succeed or both fail.
            // =====================================================

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                // =================================================
                // CREATE RESTAURANT
                // =================================================

                var restaurant =
                    new Restaurant
                    {
                        Name =
                            model.Name,

                        Address =
                            model.Address,

                        Phone =
                            model.Phone,

                        Email =
                            model.Email,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context.Restaurants.Add(
                    restaurant
                );


                await _context
                    .SaveChangesAsync();


                // =================================================
                // CREATE RESTAURANT ADMIN
                // =================================================

                var adminUser =
                    new ApplicationUser
                    {
                        UserName =
                            model.AdminEmail,

                        Email =
                            model.AdminEmail,

                        FullName =
                            model.AdminFullName,

                        RestaurantId =
                            restaurant.Id,

                        EmailConfirmed =
                            true
                    };


                var result =
                    await _userManager.CreateAsync(
                        adminUser,
                        model.AdminPassword
                    );


                if (!result.Succeeded)
                {
                    foreach (
                        var error
                        in result.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description
                        );
                    }


                    await transaction
                        .RollbackAsync();


                    return View(model);
                }


                // =================================================
                // ASSIGN RESTAURANT ADMIN ROLE
                // =================================================

                var roleResult =
                    await _userManager
                        .AddToRoleAsync(
                            adminUser,
                            "RestaurantAdmin"
                        );


                if (!roleResult.Succeeded)
                {
                    foreach (
                        var error
                        in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description
                        );
                    }


                    await transaction
                        .RollbackAsync();


                    return View(model);
                }


                // =================================================
                // COMMIT
                // =================================================

                await transaction
                    .CommitAsync();


                // =================================================
                // UPDATE SUPERADMIN ANALYTICS
                // =================================================
                //
                // Creating a restaurant changes RestaurantQR's:
                //
                // - Customer Growth graph
                // - Active / Inactive Restaurant graph
                // - Subscription Health graph
                //
                // The notification is intentionally sent AFTER the
                // database transaction has successfully committed.
                // =================================================

                await NotifySuperAdminAnalyticsChangedAsync(
                    source: "RestaurantCreated",
                    restaurantId: restaurant.Id
                );


                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (Exception ex)
            {
                await transaction
                    .RollbackAsync();


                ModelState.AddModelError(
                    "",
                    "An error occurred while saving: "
                    + ex.Message
                );


                return View(model);
            }
        }


        // =========================================================
        // ACTIVATE / DEACTIVATE RESTAURANT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id)
        {
            var restaurant =
                await _context.Restaurants
                    .FirstOrDefaultAsync(
                        r => r.Id == id
                    );


            if (restaurant == null)
            {
                return NotFound();
            }


            restaurant.IsActive =
                !restaurant.IsActive;


            await _context
                .SaveChangesAsync();


            // =====================================================
            // UPDATE SUPERADMIN ANALYTICS
            // =====================================================
            //
            // This immediately refreshes the Active vs Inactive
            // restaurant graph.
            // =====================================================

            await NotifySuperAdminAnalyticsChangedAsync(
                source:
                    restaurant.IsActive
                        ? "RestaurantActivated"
                        : "RestaurantDeactivated",

                restaurantId:
                    restaurant.Id
            );


            TempData["Success"] =
                restaurant.IsActive

                    ? "Restaurant activated successfully."

                    : "Restaurant deactivated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // GET: ASSIGN SUBSCRIPTION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            AssignSubscription(int id)
        {
            var restaurant =
                await _context.Restaurants
                    .FirstOrDefaultAsync(
                        r => r.Id == id
                    );


            if (restaurant == null)
            {
                return NotFound();
            }


            var plans =
                await _context.SubscriptionPlans

                    .Where(
                        p => p.IsActive
                    )

                    .OrderBy(
                        p => p.DurationDays
                    )

                    .ToListAsync();


            if (!plans.Any())
            {
                TempData["Error"] =
                    "No active subscription plans are available.";


                return RedirectToAction(
                    nameof(Index)
                );
            }


            var model =
                new AssignSubscriptionViewModel
                {
                    RestaurantId =
                        restaurant.Id,

                    StartDate =
                        DateTime.UtcNow.Date
                };


            ViewBag.RestaurantName =
                restaurant.Name;


            ViewBag.Plans =
                plans;


            return View(model);
        }


        // =========================================================
        // POST: ASSIGN SUBSCRIPTION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            AssignSubscription(
                AssignSubscriptionViewModel model)
        {
            // =====================================================
            // INVALID MODEL
            // =====================================================

            if (!ModelState.IsValid)
            {
                var plans =
                    await _context.SubscriptionPlans

                        .Where(
                            p => p.IsActive
                        )

                        .OrderBy(
                            p => p.DurationDays
                        )

                        .ToListAsync();


                ViewBag.Plans =
                    plans;


                var restaurant =
                    await _context.Restaurants
                        .FirstOrDefaultAsync(
                            r =>
                                r.Id ==
                                model.RestaurantId
                        );


                ViewBag.RestaurantName =
                    restaurant?.Name;


                return View(model);
            }


            // =====================================================
            // VALIDATE SELECTED PLAN
            // =====================================================

            var selectedPlan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                                model.SubscriptionPlanId
                            &&
                            p.IsActive
                    );


            if (selectedPlan == null)
            {
                ModelState.AddModelError(
                    "SubscriptionPlanId",
                    "Selected subscription plan is not available."
                );


                var plans =
                    await _context.SubscriptionPlans

                        .Where(
                            p => p.IsActive
                        )

                        .OrderBy(
                            p => p.DurationDays
                        )

                        .ToListAsync();


                ViewBag.Plans =
                    plans;


                var restaurant =
                    await _context.Restaurants
                        .FirstOrDefaultAsync(
                            r =>
                                r.Id ==
                                model.RestaurantId
                        );


                ViewBag.RestaurantName =
                    restaurant?.Name;


                return View(model);
            }


            // =====================================================
            // VALIDATE RESTAURANT
            // =====================================================

            var restaurantExists =
                await _context.Restaurants
                    .AnyAsync(
                        r =>
                            r.Id ==
                            model.RestaurantId
                    );


            if (!restaurantExists)
            {
                return NotFound();
            }


            // =====================================================
            // SUBSCRIPTION DATES
            // =====================================================

            var startDate =
                model.StartDate.Date;


            var endDate =
                startDate.AddDays(
                    selectedPlan.DurationDays
                );


            // =====================================================
            // CREATE PENDING SUBSCRIPTION
            // =====================================================
            //
            // IMPORTANT:
            //
            // SuperAdmin assigning a subscription does NOT count
            // as RestaurantQR revenue yet.
            //
            // It remains:
            //
            // PaymentStatus.Pending
            //
            // Therefore the Paid Subscription Revenue graph does
            // not increase until payment is actually marked Paid.
            // =====================================================

            var subscription =
                new Subscription
                {
                    RestaurantId =
                        model.RestaurantId,

                    SubscriptionPlanId =
                        selectedPlan.Id,

                    StartDate =
                        startDate,

                    EndDate =
                        endDate,

                    Amount =
                        selectedPlan.Price,

                    Status =
                        SubscriptionStatus.Pending,

                    PaymentStatus =
                        PaymentStatus.Pending,

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.Subscriptions.Add(
                subscription
            );


            await _context
                .SaveChangesAsync();


            // =====================================================
            // UPDATE SUPERADMIN ANALYTICS
            // =====================================================
            //
            // Revenue does NOT change here because payment is
            // pending.
            //
            // Subscription Health DOES change immediately.
            // =====================================================

            await NotifySuperAdminAnalyticsChangedAsync(
                source:
                    "SubscriptionAssigned",

                restaurantId:
                    model.RestaurantId,

                subscriptionId:
                    subscription.Id
            );


            TempData["Success"] =
                "Subscription assigned successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // SUPERADMIN ANALYTICS NOTIFICATION
        // =========================================================
        //
        // Analytics notification failure should NEVER undo a valid
        // restaurant/subscription database operation.
        //
        // SignalR is only responsible for making the dashboard
        // refresh immediately.
        //
        // The dashboard also has a 30-second fallback refresh.
        // =========================================================

        private async Task
            NotifySuperAdminAnalyticsChangedAsync(
                string source,
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

                            restaurantId,

                            subscriptionId,

                            occurredAtUtc =
                                DateTime.UtcNow
                        }
                    );
            }
            catch
            {
                // Do not fail a valid database operation just
                // because a live dashboard notification failed.
            }
        }
    }
}