using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Hubs;
using RestaurantQR.Models;
using RestaurantQR.Services;
using RestaurantQR.ViewModels;
using System.Security.Cryptography;

namespace RestaurantQR.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly IEmailService _emailService;
        private readonly ILogger<SubscriptionController> _logger;


        public SubscriptionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<OrderHub> orderHub,
            IEmailService emailService,
            ILogger<SubscriptionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _orderHub = orderHub;
            _emailService = emailService;
            _logger = logger;
        }


        // =========================================================
        // GET: /Subscription/Buy?planId=1
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Buy(int planId)
        {
            var plan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p =>
                        p.Id == planId &&
                        p.IsActive);


            if (plan == null)
            {
                return NotFound();
            }


            var model =
                new PurchaseSubscriptionViewModel
                {
                    SubscriptionPlanId =
                        plan.Id,

                    PlanName =
                        plan.Name,

                    Amount =
                        plan.Price,

                    DurationDays =
                        plan.DurationDays,

                    StartDate =
                        DateTime.UtcNow.Date
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


            var plan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p =>
                        p.Id ==
                            model.SubscriptionPlanId &&
                        p.IsActive);


            if (plan == null)
            {
                return NotFound();
            }


            // =====================================================
            // NEVER TRUST PRICE FROM THE BROWSER
            // =====================================================

            model.PlanName =
                plan.Name;

            model.Amount =
                plan.Price;

            model.DurationDays =
                plan.DurationDays;


            return View(
                "PaymentCheckout",
                model);
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


            var plan =
                await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p =>
                        p.Id ==
                            model.SubscriptionPlanId &&
                        p.IsActive);


            if (plan == null)
            {
                return NotFound();
            }


            // =====================================================
            // ALWAYS USE DATABASE PLAN VALUES
            // =====================================================

            model.PlanName =
                plan.Name;

            model.Amount =
                plan.Price;

            model.DurationDays =
                plan.DurationDays;


            // =====================================================
            // PAYMENT
            // =====================================================
            //
            // Payment is currently simulated in this project.
            // Razorpay can be integrated later.
            // =====================================================

            if (model.PaymentMethod == null)
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethod),
                    "Please select a payment method."
                );


                return View(model);
            }


            // =====================================================
            // PREVENT DUPLICATE ADMIN EMAIL
            // =====================================================

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.Email
                );


            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account already exists with this email address."
                );


                return View(model);
            }


            // =====================================================
            // DATABASE TRANSACTION
            // =====================================================
            //
            // Restaurant + Subscription + Admin account should
            // either all be created successfully or none of them.
            // =====================================================

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            Restaurant? restaurant =
                null;


            Subscription? subscription =
                null;


            ApplicationUser? adminUser =
                null;


            string temporaryPassword =
                string.Empty;


            try
            {
                // =================================================
                // CREATE RESTAURANT
                // =================================================
                //
                // IMPORTANT:
                //
                // Restaurant remains disabled until SuperAdmin
                // confirms the paid subscription.
                // =================================================

                restaurant =
                    new Restaurant
                    {
                        Name =
                            model.RestaurantName,

                        Address =
                            model.Address,

                        Phone =
                            model.Phone,

                        Email =
                            model.Email,

                        IsActive =
                            false,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context.Restaurants.Add(
                    restaurant
                );


                await _context
                    .SaveChangesAsync();


                // =================================================
                // CREATE PAID BUT PENDING SUBSCRIPTION
                // =================================================
                //
                // PaymentStatus = Paid
                // SubscriptionStatus = Pending
                //
                // SuperAdmin must confirm it before activation.
                // =================================================

                subscription =
                    new Subscription
                    {
                        RestaurantId =
                            restaurant.Id,

                        SubscriptionPlanId =
                            plan.Id,

                        StartDate =
                            model.StartDate,

                        EndDate =
                            model.StartDate
                                .AddDays(
                                    plan.DurationDays
                                ),

                        Amount =
                            plan.Price,

                        Status =
                            SubscriptionStatus.Pending,

                        PaymentStatus =
                            PaymentStatus.Paid,

                        PaymentMethod =
                            model.PaymentMethod,

                        PaidAt =
                            DateTime.UtcNow,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context.Subscriptions.Add(
                    subscription
                );


                await _context
                    .SaveChangesAsync();


                // =================================================
                // CREATE RESTAURANT ADMIN ACCOUNT
                // =================================================

                adminUser =
                    new ApplicationUser
                    {
                        UserName =
                            model.Email,

                        Email =
                            model.Email,

                        FullName =
                            model.OwnerName,

                        PhoneNumber =
                            model.Phone,

                        EmailConfirmed =
                            true,

                        RestaurantId =
                            restaurant.Id,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                temporaryPassword =
                    GenerateTemporaryPassword();


                var userResult =
                    await _userManager.CreateAsync(
                        adminUser,
                        temporaryPassword
                    );


                if (!userResult.Succeeded)
                {
                    await transaction
                        .RollbackAsync();


                    var errors =
                        string.Join(
                            ", ",
                            userResult.Errors
                                .Select(
                                    e => e.Description
                                )
                        );


                    ModelState.AddModelError(
                        string.Empty,
                        $"Admin account could not be created: {errors}"
                    );


                    return View(model);
                }


                // =================================================
                // ASSIGN RESTAURANT ADMIN ROLE
                // =================================================

                var roleResult =
                    await _userManager.AddToRoleAsync(
                        adminUser,
                        "RestaurantAdmin"
                    );


                if (!roleResult.Succeeded)
                {
                    await transaction
                        .RollbackAsync();


                    var errors =
                        string.Join(
                            ", ",
                            roleResult.Errors
                                .Select(
                                    e => e.Description
                                )
                        );


                    ModelState.AddModelError(
                        string.Empty,
                        $"Restaurant Admin role could not be assigned: {errors}"
                    );


                    return View(model);
                }


                // =================================================
                // COMMIT DATABASE CHANGES
                // =================================================

                await transaction
                    .CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction
                    .RollbackAsync();


                _logger.LogError(
                    ex,
                    "Subscription purchase failed for {Email}.",
                    model.Email
                );


                ModelState.AddModelError(
                    string.Empty,
                    "Something went wrong while creating your subscription. Please try again."
                );


                return View(model);
            }


            // =====================================================
            // SEND PAYMENT RECEIVED + TEMP PASSWORD EMAIL
            // =====================================================
            //
            // IMPORTANT:
            //
            // Email is sent AFTER database commit.
            //
            // If Gmail/SMTP is temporarily unavailable, the paid
            // subscription must NOT be cancelled or rolled back.
            // =====================================================

            try
            {
                await _emailService
                    .SendPaymentReceivedEmailAsync(
                        recipientEmail:
                            model.Email,

                        ownerName:
                            model.OwnerName,

                        restaurantName:
                            model.RestaurantName,

                        planName:
                            plan.Name,

                        adminEmail:
                            adminUser!.Email
                                ?? model.Email,

                        temporaryPassword:
                            temporaryPassword
                    );


                TempData["EmailSent"] =
                    true;
            }
            catch (Exception ex)
            {
                // =================================================
                // PAYMENT/ACCOUNT IS STILL VALID
                // =================================================

                _logger.LogError(
                    ex,
                    "Payment succeeded but temporary credentials email failed for {Email}.",
                    model.Email
                );


                TempData["EmailSent"] =
                    false;


                TempData["EmailWarning"] =
                    "Payment was successful, but the confirmation email could not be sent. Please keep the temporary credentials shown below.";
            }


            // =====================================================
            // NOTIFY SUPERADMIN ANALYTICS
            // =====================================================
            //
            // This is now a PAID + PENDING subscription.
            //
            // The restaurant will only become Active after
            // SuperAdmin approval.
            // =====================================================

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
                            source =
                                "SubscriptionPurchasedPendingApproval",

                            restaurantId =
                                restaurant!.Id,

                            subscriptionId =
                                subscription!.Id,

                            planId =
                                plan.Id,

                            occurredAtUtc =
                                DateTime.UtcNow
                        }
                    );
            }
            catch (Exception ex)
            {
                // SignalR notification failure must never affect
                // successful payment/account creation.

                _logger.LogWarning(
                    ex,
                    "SuperAdmin analytics notification failed for subscription {SubscriptionId}.",
                    subscription!.Id
                );
            }


            // =====================================================
            // PAYMENT SUCCESS
            // =====================================================

            TempData["AdminEmail"] =
                adminUser!.Email;


            TempData["TemporaryPassword"] =
                temporaryPassword;


            TempData["ApprovalPending"] =
                true;


            return RedirectToAction(
                nameof(Success),
                new
                {
                    subscriptionId =
                        subscription!.Id
                }
            );
        }


        // =========================================================
        // GET: /Subscription/PaymentCheckout
        // =========================================================

        [HttpGet]
        public IActionResult PaymentCheckout()
        {
            return RedirectToAction(
                nameof(Buy)
            );
        }


        // =========================================================
        // GET: /Subscription/Success
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Success(
            int subscriptionId)
        {
            var subscription =
                await _context.Subscriptions

                    .Include(s =>
                        s.Restaurant)

                    .Include(s =>
                        s.SubscriptionPlan)

                    .FirstOrDefaultAsync(s =>
                        s.Id ==
                            subscriptionId);


            if (subscription == null)
            {
                return NotFound();
            }


            var model =
                new SubscriptionSuccessViewModel
                {
                    Subscription =
                        subscription,

                    AdminEmail =
                        TempData["AdminEmail"]
                            ?.ToString()
                        ?? string.Empty,

                    TemporaryPassword =
                        TempData["TemporaryPassword"]
                            ?.ToString()
                        ?? string.Empty
                };


            ViewBag.EmailSent =
                TempData["EmailSent"];


            ViewBag.EmailWarning =
                TempData["EmailWarning"]
                    ?.ToString();


            ViewBag.ApprovalPending =
                TempData["ApprovalPending"];


            return View(model);
        }


        // =========================================================
        // TEMPORARY PASSWORD GENERATOR
        // =========================================================

        private static string
            GenerateTemporaryPassword()
        {
            const string upper =
                "ABCDEFGHJKLMNPQRSTUVWXYZ";

            const string lower =
                "abcdefghijkmnopqrstuvwxyz";

            const string digits =
                "23456789";


            using var random =
                RandomNumberGenerator.Create();


            string GetRandomChar(
                string chars)
            {
                var bytes =
                    new byte[4];


                random.GetBytes(
                    bytes
                );


                var index =
                    BitConverter
                        .ToUInt32(
                            bytes,
                            0
                        )
                    % chars.Length;


                return chars[
                    (int)index
                ].ToString();
            }


            return
                GetRandomChar(upper)
                +
                GetRandomChar(lower)
                +
                GetRandomChar(digits)
                +
                GetRandomChar(
                    upper +
                    lower +
                    digits
                )
                +
                GetRandomChar(
                    upper +
                    lower +
                    digits
                )
                +
                GetRandomChar(
                    upper +
                    lower +
                    digits
                )
                +
                GetRandomChar(
                    upper +
                    lower +
                    digits
                )
                +
                GetRandomChar(
                    upper +
                    lower +
                    digits
                );
        }
    }
}