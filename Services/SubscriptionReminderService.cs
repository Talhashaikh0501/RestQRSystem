using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;

namespace RestaurantQR.Services
{
    public class SubscriptionReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionReminderService> _logger;

        public SubscriptionReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        // =========================================================
        // BACKGROUND LOOP
        // =========================================================
        //
        // We check every hour instead of exactly once every day.
        //
        // The SubscriptionReminderLog table prevents the same
        // 7/6/5/4/3/2/1-day email from being sent twice.
        //
        // This also means if the server restarts during the day,
        // it can still catch the reminder later.
        // =========================================================

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            // Small startup delay so the web app/database has time
            // to finish starting.
            await Task.Delay(
                TimeSpan.FromSeconds(20),
                stoppingToken
            );


            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSubscriptionsAsync(
                        stoppingToken
                    );
                }
                catch (OperationCanceledException)
                {
                    // Normal during application shutdown.
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error in SubscriptionReminderService."
                    );
                }


                // Check again every hour.
                await Task.Delay(
                    TimeSpan.FromHours(1),
                    stoppingToken
                );
            }
        }


        // =========================================================
        // PROCESS SUBSCRIPTIONS
        // =========================================================

        private async Task ProcessSubscriptionsAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();


            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();


            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<IEmailService>();


            var today =
                DateTime.UtcNow.Date;


            // =====================================================
            // GET ALL PAID ACTIVE SUBSCRIPTIONS
            // =====================================================

            var subscriptions =
                await context.Subscriptions

                    .Include(s => s.Restaurant)

                    .Include(s => s.SubscriptionPlan)

                    .Where(s =>
                        s.PaymentStatus ==
                            PaymentStatus.Paid
                        &&
                        s.Status ==
                            SubscriptionStatus.Active
                    )

                    .ToListAsync(
                        cancellationToken
                    );


            foreach (var subscription in subscriptions)
            {
                try
                {
                    await ProcessSingleSubscriptionAsync(
                        context,
                        emailService,
                        subscription,
                        today,
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    // One restaurant failing should never stop
                    // reminders for every other restaurant.

                    _logger.LogError(
                        ex,
                        "Reminder processing failed for Subscription {SubscriptionId}.",
                        subscription.Id
                    );
                }
            }
        }


        // =========================================================
        // PROCESS ONE SUBSCRIPTION
        // =========================================================

        private async Task ProcessSingleSubscriptionAsync(
            ApplicationDbContext context,
            IEmailService emailService,
            Subscription subscription,
            DateTime today,
            CancellationToken cancellationToken)
        {
            if (subscription.Restaurant == null ||
                subscription.SubscriptionPlan == null)
            {
                return;
            }


            var expiryDate =
                subscription.EndDate.Date;


            var daysRemaining =
                (expiryDate - today).Days;


            // =====================================================
            // FIND RESTAURANT ADMIN EMAIL
            // =====================================================

            var adminUser =
                await context.Users

                    .AsNoTracking()

                    .Where(u =>
                        u.RestaurantId ==
                            subscription.RestaurantId
                    )

                    .OrderByDescending(u =>
                        u.Email ==
                            subscription.Restaurant.Email
                    )

                    .ThenBy(u =>
                        u.CreatedAt
                    )

                    .FirstOrDefaultAsync(
                        cancellationToken
                    );


            var recipientEmail =
                adminUser?.Email;


            if (string.IsNullOrWhiteSpace(
                recipientEmail))
            {
                recipientEmail =
                    subscription.Restaurant.Email;
            }


            if (string.IsNullOrWhiteSpace(
                recipientEmail))
            {
                _logger.LogWarning(
                    "No email address found for Restaurant {RestaurantId}.",
                    subscription.RestaurantId
                );

                return;
            }


            var ownerName =
                adminUser?.FullName;


            if (string.IsNullOrWhiteSpace(
                ownerName))
            {
                ownerName =
                    "Restaurant Administrator";
            }


            // =====================================================
            // 7, 6, 5, 4, 3, 2, 1 DAYS REMAINING
            // =====================================================

            if (daysRemaining >= 1 &&
                daysRemaining <= 7)
            {
                await SendExpiryReminderIfNeededAsync(
                    context,
                    emailService,
                    subscription,
                    recipientEmail,
                    ownerName,
                    daysRemaining,
                    cancellationToken
                );

                return;
            }


            // =====================================================
            // SUBSCRIPTION EXPIRED
            // =====================================================
            //
            // Existing project logic considers the subscription
            // valid through EndDate.
            //
            // Example:
            // EndDate = 10 September
            //
            // 10 September -> still valid
            // 11 September -> expired
            // =====================================================

            if (daysRemaining < 0)
            {
                await ExpireSubscriptionAsync(
                    context,
                    emailService,
                    subscription,
                    recipientEmail,
                    ownerName,
                    cancellationToken
                );
            }
        }


        // =========================================================
        // SEND DAILY REMINDER
        // =========================================================

        private async Task SendExpiryReminderIfNeededAsync(
            ApplicationDbContext context,
            IEmailService emailService,
            Subscription subscription,
            string recipientEmail,
            string ownerName,
            int daysRemaining,
            CancellationToken cancellationToken)
        {
            const string reminderType =
                "ExpiryReminder";


            // =====================================================
            // CHECK IF TODAY'S REMINDER WAS ALREADY SENT
            // =====================================================

            var alreadySent =
                await context.SubscriptionReminderLogs

                    .AsNoTracking()

                    .AnyAsync(
                        r =>
                            r.SubscriptionId ==
                                subscription.Id
                            &&
                            r.DaysRemaining ==
                                daysRemaining
                            &&
                            r.ReminderType ==
                                reminderType,

                        cancellationToken
                    );


            if (alreadySent)
            {
                return;
            }


            // =====================================================
            // SEND EMAIL
            // =====================================================

            await emailService
                .SendSubscriptionExpiryReminderEmailAsync(
                    recipientEmail:
                        recipientEmail,

                    ownerName:
                        ownerName,

                    restaurantName:
                        subscription.Restaurant.Name,

                    planName:
                        subscription.SubscriptionPlan.Name,

                    daysRemaining:
                        daysRemaining,

                    expiryDate:
                        subscription.EndDate,

                    renewalAmount:
                        subscription.SubscriptionPlan.Price
                );


            // =====================================================
            // SAVE REMINDER HISTORY
            // =====================================================

            context.SubscriptionReminderLogs.Add(
                new SubscriptionReminderLog
                {
                    SubscriptionId =
                        subscription.Id,

                    DaysRemaining =
                        daysRemaining,

                    ReminderType =
                        reminderType,

                    SentAt =
                        DateTime.UtcNow
                }
            );


            await context.SaveChangesAsync(
                cancellationToken
            );


            _logger.LogInformation(
                "{DaysRemaining}-day subscription reminder sent for Subscription {SubscriptionId}.",
                daysRemaining,
                subscription.Id
            );
        }


        // =========================================================
        // EXPIRE + DISABLE ACCOUNT
        // =========================================================

        private async Task ExpireSubscriptionAsync(
            ApplicationDbContext context,
            IEmailService emailService,
            Subscription subscription,
            string recipientEmail,
            string ownerName,
            CancellationToken cancellationToken)
        {
            const string reminderType =
                "Expired";


            // =====================================================
            // DISABLE SUBSCRIPTION
            // =====================================================

            subscription.Status =
                SubscriptionStatus.Expired;


            subscription.UpdatedAt =
                DateTime.UtcNow;


            // =====================================================
            // DISABLE RESTAURANT
            // =====================================================

            if (subscription.Restaurant != null)
            {
                subscription.Restaurant.IsActive =
                    false;
            }


            // Save account deactivation FIRST.
            //
            // Even if SMTP is temporarily unavailable, an expired
            // subscription must not continue to have access.

            await context.SaveChangesAsync(
                cancellationToken
            );


            // =====================================================
            // CHECK EXPIRED EMAIL
            // =====================================================

            var expiredEmailAlreadySent =
                await context.SubscriptionReminderLogs

                    .AsNoTracking()

                    .AnyAsync(
                        r =>
                            r.SubscriptionId ==
                                subscription.Id
                            &&
                            r.DaysRemaining ==
                                0
                            &&
                            r.ReminderType ==
                                reminderType,

                        cancellationToken
                    );


            if (expiredEmailAlreadySent)
            {
                return;
            }


            // =====================================================
            // SEND FINAL EXPIRED EMAIL
            // =====================================================

            try
            {
                await emailService
                    .SendSubscriptionExpiredEmailAsync(
                        recipientEmail:
                            recipientEmail,

                        ownerName:
                            ownerName,

                        restaurantName:
                            subscription.Restaurant.Name,

                        planName:
                            subscription.SubscriptionPlan.Name,

                        expiryDate:
                            subscription.EndDate
                    );


                context.SubscriptionReminderLogs.Add(
                    new SubscriptionReminderLog
                    {
                        SubscriptionId =
                            subscription.Id,

                        DaysRemaining =
                            0,

                        ReminderType =
                            reminderType,

                        SentAt =
                            DateTime.UtcNow
                    }
                );


                await context.SaveChangesAsync(
                    cancellationToken
                );


                _logger.LogInformation(
                    "Subscription {SubscriptionId} expired. Restaurant disabled and expiry email sent.",
                    subscription.Id
                );
            }
            catch (Exception ex)
            {
                // Account remains disabled even if email fails.

                _logger.LogError(
                    ex,
                    "Subscription {SubscriptionId} expired and was disabled, but expiry email could not be sent.",
                    subscription.Id
                );
            }
        }
    }
}