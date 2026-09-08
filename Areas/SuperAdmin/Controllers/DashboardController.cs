using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string range = "30d")
        {
            var model = await BuildDashboardModelAsync(range);

            return View(model);
        }


        // =========================================================
        // LIVE ANALYTICS ENDPOINT
        // =========================================================
        //
        // The dashboard JavaScript will call this endpoint whenever:
        //
        // 1. SuperAdmin changes the revenue range.
        // 2. SignalR tells the dashboard that platform data changed.
        // 3. The fallback automatic refresh runs.
        //
        // This means the graphs receive fresh SQL data without
        // refreshing the entire browser page.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Analytics(string range = "30d")
        {
            var model = await BuildDashboardModelAsync(range);

            return Json(model);
        }


        // =========================================================
        // BUILD COMPLETE DASHBOARD
        // =========================================================

        private async Task<SuperAdminDashboardViewModel>
            BuildDashboardModelAsync(string range)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var normalizedRange = NormalizeRevenueRange(range);


            // =====================================================
            // RESTAURANTS
            // =====================================================

            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .ToListAsync();


            // =====================================================
            // SUBSCRIPTIONS
            // =====================================================
            //
            // SubscriptionPlan is included because the dashboard
            // needs to show the plan distribution graph.
            // =====================================================

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.SubscriptionPlan)
                .ToListAsync();


            // =====================================================
            // PLATFORM ORDERS
            // =====================================================
            //
            // We only need the latest 14 days for the platform
            // usage graph.
            // =====================================================

            var platformUsageStart = today.AddDays(-13);

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= platformUsageStart)
                .Select(o => new
                {
                    o.CreatedAt
                })
                .ToListAsync();


            // =====================================================
            // PAID SUBSCRIPTIONS
            // =====================================================
            //
            // Revenue is ONLY calculated from PaymentStatus.Paid.
            //
            // Pending / Failed / Cancelled / Refunded payments do
            // not increase RestaurantQR's subscription revenue.
            // =====================================================

            var paidSubscriptions = subscriptions
                .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                .ToList();


            // =====================================================
            // LATEST SUBSCRIPTION FOR EACH RESTAURANT
            // =====================================================
            //
            // A restaurant may have multiple historical
            // subscriptions.
            //
            // Example:
            //
            // Restaurant A
            // ├── Old 6 Month Subscription
            // ├── Old 1 Year Subscription
            // └── Current Subscription
            //
            // Dashboard health graphs should count the restaurant
            // once, using its latest subscription.
            // =====================================================

            var latestSubscriptionByRestaurant = subscriptions
                .GroupBy(s => s.RestaurantId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(s => s.StartDate)
                        .ThenByDescending(s => s.CreatedAt)
                        .ThenByDescending(s => s.Id)
                        .First()
                );


            // =====================================================
            // BASE VIEW MODEL
            // =====================================================

            var model = new SuperAdminDashboardViewModel
            {
                GeneratedAtUtc = now,

                SelectedRevenueRange = normalizedRange,

                SelectedRevenueRangeLabel =
                    GetRevenueRangeLabel(normalizedRange),

                TotalPaidSubscriptionRevenue =
                    paidSubscriptions.Sum(s => s.Amount)
            };


            // =====================================================
            // BUILD ALL GRAPH DATA
            // =====================================================

            BuildRevenueTrend(
                model,
                paidSubscriptions,
                normalizedRange,
                now
            );


            BuildRestaurantGrowthTrend(
                model,
                restaurants,
                today
            );


            BuildRestaurantStatusDistribution(
                model,
                restaurants
            );


            BuildSubscriptionStatusDistribution(
                model,
                restaurants,
                latestSubscriptionByRestaurant,
                now
            );


            BuildSubscriptionPlanDistribution(
                model,
                latestSubscriptionByRestaurant,
                now
            );


            BuildSubscriptionExpiryTrend(
                model,
                latestSubscriptionByRestaurant,
                today,
                now
            );


            BuildPlatformUsageTrend(
                model,
                recentOrders.Select(o => o.CreatedAt),
                today
            );


            return model;
        }


        // =========================================================
        // 1. PAID SUBSCRIPTION REVENUE GRAPH
        // =========================================================
        //
        // Supported ranges:
        //
        // 30d = Last 30 Days
        // 6m  = Last 6 Months
        // 1y  = Last 1 Year
        //
        // 30 days = daily points
        // 6 months = monthly points
        // 1 year = monthly points
        // =========================================================

        private static void BuildRevenueTrend(
            SuperAdminDashboardViewModel model,
            IReadOnlyCollection<Subscription> paidSubscriptions,
            string range,
            DateTime now)
        {
            // =====================================================
            // LAST 6 MONTHS
            // =====================================================

            if (range == "6m")
            {
                var firstMonth =
                    new DateTime(now.Year, now.Month, 1)
                        .AddMonths(-5);


                for (var i = 0; i < 6; i++)
                {
                    var monthStart =
                        firstMonth.AddMonths(i);

                    var monthEnd =
                        monthStart.AddMonths(1);


                    var value = paidSubscriptions
                        .Where(s =>
                        {
                            var paidDate = GetPaidDate(s);

                            return
                                paidDate >= monthStart &&
                                paidDate < monthEnd &&
                                paidDate <= now;
                        })
                        .Sum(s => s.Amount);


                    model.SubscriptionRevenueTrend.Add(
                        new SuperAdminDashboardPoint
                        {
                            Label =
                                monthStart.ToString("MMM yyyy"),

                            Value = value
                        }
                    );
                }
            }

            // =====================================================
            // LAST 1 YEAR
            // =====================================================

            else if (range == "1y")
            {
                var firstMonth =
                    new DateTime(now.Year, now.Month, 1)
                        .AddMonths(-11);


                for (var i = 0; i < 12; i++)
                {
                    var monthStart =
                        firstMonth.AddMonths(i);

                    var monthEnd =
                        monthStart.AddMonths(1);


                    var value = paidSubscriptions
                        .Where(s =>
                        {
                            var paidDate = GetPaidDate(s);

                            return
                                paidDate >= monthStart &&
                                paidDate < monthEnd &&
                                paidDate <= now;
                        })
                        .Sum(s => s.Amount);


                    model.SubscriptionRevenueTrend.Add(
                        new SuperAdminDashboardPoint
                        {
                            Label =
                                monthStart.ToString("MMM yyyy"),

                            Value = value
                        }
                    );
                }
            }

            // =====================================================
            // LAST 30 DAYS
            // =====================================================

            else
            {
                var firstDay =
                    now.Date.AddDays(-29);


                for (var i = 0; i < 30; i++)
                {
                    var dayStart =
                        firstDay.AddDays(i);

                    var dayEnd =
                        dayStart.AddDays(1);


                    var value = paidSubscriptions
                        .Where(s =>
                        {
                            var paidDate = GetPaidDate(s);

                            return
                                paidDate >= dayStart &&
                                paidDate < dayEnd &&
                                paidDate <= now;
                        })
                        .Sum(s => s.Amount);


                    model.SubscriptionRevenueTrend.Add(
                        new SuperAdminDashboardPoint
                        {
                            Label =
                                dayStart.ToString("dd MMM"),

                            Value = value
                        }
                    );
                }
            }


            // =====================================================
            // TOTAL FOR SELECTED PERIOD
            // =====================================================

            model.SelectedPaidSubscriptionRevenue =
                model.SubscriptionRevenueTrend
                    .Sum(point => point.Value);
        }


        // =========================================================
        // 2. RESTAURANTQR CUSTOMER GROWTH GRAPH
        // =========================================================
        //
        // This is NOT restaurant business revenue.
        //
        // It shows how RestaurantQR itself is growing by tracking
        // the cumulative number of restaurants registered on the
        // platform.
        // =========================================================

        private static void BuildRestaurantGrowthTrend(
            SuperAdminDashboardViewModel model,
            IReadOnlyCollection<Restaurant> restaurants,
            DateTime today)
        {
            var currentMonth =
                new DateTime(
                    today.Year,
                    today.Month,
                    1
                );


            var firstMonth =
                currentMonth.AddMonths(-11);


            for (var i = 0; i < 12; i++)
            {
                var monthStart =
                    firstMonth.AddMonths(i);

                var nextMonth =
                    monthStart.AddMonths(1);


                var cumulativeRestaurants =
                    restaurants.Count(
                        r => r.CreatedAt < nextMonth
                    );


                model.RestaurantGrowthTrend.Add(
                    new SuperAdminDashboardPoint
                    {
                        Label =
                            monthStart.ToString("MMM yyyy"),

                        Value =
                            cumulativeRestaurants
                    }
                );
            }
        }


        // =========================================================
        // 3. RESTAURANT ACTIVITY GRAPH
        // =========================================================
        //
        // Shows RestaurantQR customers that are currently enabled
        // versus disabled.
        // =========================================================

        private static void BuildRestaurantStatusDistribution(
            SuperAdminDashboardViewModel model,
            IReadOnlyCollection<Restaurant> restaurants)
        {
            model.RestaurantStatusDistribution.Add(
                new SuperAdminDashboardPoint
                {
                    Label = "Active",

                    Value =
                        restaurants.Count(
                            r => r.IsActive
                        )
                }
            );


            model.RestaurantStatusDistribution.Add(
                new SuperAdminDashboardPoint
                {
                    Label = "Inactive",

                    Value =
                        restaurants.Count(
                            r => !r.IsActive
                        )
                }
            );
        }


        // =========================================================
        // 4. SUBSCRIPTION HEALTH GRAPH
        // =========================================================
        //
        // Each restaurant is counted only once.
        //
        // Possible groups:
        //
        // Active
        // Pending
        // Expired
        // Cancelled
        // Suspended
        // No Subscription
        // =========================================================

        private static void BuildSubscriptionStatusDistribution(
            SuperAdminDashboardViewModel model,
            IReadOnlyCollection<Restaurant> restaurants,
            IReadOnlyDictionary<int, Subscription>
                latestSubscriptionByRestaurant,
            DateTime now)
        {
            var counts =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    ["Active"] = 0,
                    ["Pending"] = 0,
                    ["Expired"] = 0,
                    ["Cancelled"] = 0,
                    ["Suspended"] = 0,
                    ["No Subscription"] = 0
                };


            foreach (var restaurant in restaurants)
            {
                if (!latestSubscriptionByRestaurant
                    .TryGetValue(
                        restaurant.Id,
                        out var subscription
                    ))
                {
                    counts["No Subscription"]++;

                    continue;
                }


                var effectiveStatus =
                    GetEffectiveSubscriptionStatus(
                        subscription,
                        now
                    );


                counts[effectiveStatus]++;
            }


            foreach (var item in counts)
            {
                model.SubscriptionStatusDistribution.Add(
                    new SuperAdminDashboardPoint
                    {
                        Label = item.Key,
                        Value = item.Value
                    }
                );
            }
        }


        // =========================================================
        // 5. SUBSCRIPTION PLAN MIX GRAPH
        // =========================================================
        //
        // Shows which PAID + ACTIVE plans RestaurantQR customers
        // are currently using.
        //
        // Historical expired plans are not counted.
        // =========================================================

        private static void BuildSubscriptionPlanDistribution(
            SuperAdminDashboardViewModel model,
            IReadOnlyDictionary<int, Subscription>
                latestSubscriptionByRestaurant,
            DateTime now)
        {
            var planCounts =
                latestSubscriptionByRestaurant
                    .Values

                    .Where(s =>
                        GetEffectiveSubscriptionStatus(
                            s,
                            now
                        ) == "Active"

                        &&

                        s.PaymentStatus ==
                        PaymentStatus.Paid
                    )

                    .GroupBy(
                        s =>
                            s.SubscriptionPlan?.Name
                            ?? "Unknown Plan"
                    )

                    .Select(group => new
                    {
                        Name = group.Key,
                        Count = group.Count()
                    })

                    .OrderByDescending(
                        x => x.Count
                    )

                    .ThenBy(
                        x => x.Name
                    )

                    .ToList();


            foreach (var plan in planCounts)
            {
                model.SubscriptionPlanDistribution.Add(
                    new SuperAdminDashboardPoint
                    {
                        Label = plan.Name,
                        Value = plan.Count
                    }
                );
            }
        }


        // =========================================================
        // 6. UPCOMING SUBSCRIPTION EXPIRATIONS GRAPH
        // =========================================================
        //
        // Shows how many currently active paid subscriptions will
        // expire in each of the next six calendar months.
        // =========================================================

        private static void BuildSubscriptionExpiryTrend(
            SuperAdminDashboardViewModel model,
            IReadOnlyDictionary<int, Subscription>
                latestSubscriptionByRestaurant,
            DateTime today,
            DateTime now)
        {
            var firstMonth =
                new DateTime(
                    today.Year,
                    today.Month,
                    1
                );


            var activePaidSubscriptions =
                latestSubscriptionByRestaurant
                    .Values

                    .Where(s =>
                        GetEffectiveSubscriptionStatus(
                            s,
                            now
                        ) == "Active"

                        &&

                        s.PaymentStatus ==
                        PaymentStatus.Paid
                    )

                    .ToList();


            for (var i = 0; i < 6; i++)
            {
                var monthStart =
                    firstMonth.AddMonths(i);

                var monthEnd =
                    monthStart.AddMonths(1);


                var expiringCount =
                    activePaidSubscriptions.Count(
                        s =>
                            s.EndDate >= today &&
                            s.EndDate >= monthStart &&
                            s.EndDate < monthEnd
                    );


                model.SubscriptionExpiryTrend.Add(
                    new SuperAdminDashboardPoint
                    {
                        Label =
                            monthStart.ToString("MMM yyyy"),

                        Value =
                            expiringCount
                    }
                );
            }
        }


        // =========================================================
        // 7. PLATFORM USAGE GRAPH
        // =========================================================
        //
        // This does NOT show restaurant revenue.
        //
        // It shows how much RestaurantQR's ordering platform is
        // actually being used across all restaurants.
        //
        // The graph contains the number of orders processed by the
        // platform for each of the latest 14 days.
        // =========================================================

        private static void BuildPlatformUsageTrend(
            SuperAdminDashboardViewModel model,
            IEnumerable<DateTime> orderDates,
            DateTime today)
        {
            var dates =
                orderDates.ToList();


            var firstDay =
                today.AddDays(-13);


            for (var i = 0; i < 14; i++)
            {
                var dayStart =
                    firstDay.AddDays(i);

                var dayEnd =
                    dayStart.AddDays(1);


                var orderCount =
                    dates.Count(
                        date =>
                            date >= dayStart &&
                            date < dayEnd
                    );


                model.PlatformUsageTrend.Add(
                    new SuperAdminDashboardPoint
                    {
                        Label =
                            dayStart.ToString("dd MMM"),

                        Value =
                            orderCount
                    }
                );
            }
        }


        // =========================================================
        // PAYMENT DATE
        // =========================================================
        //
        // PaidAt is the correct business date for revenue.
        //
        // CreatedAt is used only as a fallback for any older paid
        // record where PaidAt was not populated.
        // =========================================================

        private static DateTime GetPaidDate(
            Subscription subscription)
        {
            return
                subscription.PaidAt
                ?? subscription.CreatedAt;
        }


        // =========================================================
        // EFFECTIVE SUBSCRIPTION STATUS
        // =========================================================
        //
        // This prevents a database record from still appearing as
        // "Active" when its EndDate has already passed.
        // =========================================================

        private static string
            GetEffectiveSubscriptionStatus(
                Subscription subscription,
                DateTime now)
        {
            if (
                subscription.Status
                    != SubscriptionStatus.Cancelled

                &&

                subscription.Status
                    != SubscriptionStatus.Suspended

                &&

                subscription.EndDate < now
            )
            {
                return "Expired";
            }


            return subscription.Status switch
            {
                SubscriptionStatus.Active
                    => "Active",

                SubscriptionStatus.Pending
                    => "Pending",

                SubscriptionStatus.Expired
                    => "Expired",

                SubscriptionStatus.Cancelled
                    => "Cancelled",

                SubscriptionStatus.Suspended
                    => "Suspended",

                _ => "Pending"
            };
        }


        // =========================================================
        // REVENUE RANGE VALIDATION
        // =========================================================
        //
        // Invalid URL values automatically fall back to 30 days.
        //
        // Example:
        //
        // ?range=anything
        //
        // becomes:
        //
        // 30d
        // =========================================================

        private static string NormalizeRevenueRange(
            string? range)
        {
            return range?
                .Trim()
                .ToLowerInvariant()
                switch
            {
                "6m" => "6m",
                "1y" => "1y",
                _ => "30d"
            };
        }


        // =========================================================
        // DISPLAY LABEL
        // =========================================================

        private static string GetRevenueRangeLabel(
            string range)
        {
            return range switch
            {
                "6m" => "Last 6 Months",

                "1y" => "Last 1 Year",

                _ => "Last 30 Days"
            };
        }
    }
}