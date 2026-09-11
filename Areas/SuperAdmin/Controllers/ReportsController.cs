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
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? restaurantId,
            string? status,
            string? dateRange,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var monthStart = new DateTime(
                now.Year,
                now.Month,
                1,
                0, 0, 0,
                DateTimeKind.Utc);

            var nextMonthStart = monthStart.AddMonths(1);

            // ----------------------------------------------------
            // RESTAURANTS
            // ----------------------------------------------------

            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();

            // ----------------------------------------------------
            // SUBSCRIPTIONS
            // ----------------------------------------------------

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Restaurant)
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // ----------------------------------------------------
            // OWNERS
            // ----------------------------------------------------

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.RestaurantId != null)
                .ToListAsync();

            // ----------------------------------------------------
            // DATE FILTER
            // ----------------------------------------------------

            DateTime? filterFrom = null;
            DateTime? filterTo = null;

            switch ((dateRange ?? "all").ToLower())
            {
                case "today":
                    filterFrom = today;
                    filterTo = today.AddDays(1).AddTicks(-1);
                    break;

                case "7days":
                    filterFrom = today.AddDays(-6);
                    filterTo = today.AddDays(1).AddTicks(-1);
                    break;

                case "30days":
                    filterFrom = today.AddDays(-29);
                    filterTo = today.AddDays(1).AddTicks(-1);
                    break;

                case "month":
                    filterFrom = monthStart;
                    filterTo = nextMonthStart.AddTicks(-1);
                    break;

                case "custom":
                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        filterFrom = fromDate.Value.Date;
                        filterTo = toDate.Value.Date
                            .AddDays(1)
                            .AddTicks(-1);
                    }
                    break;
            }

            // ----------------------------------------------------
            // NORMALIZE STATUS
            // ----------------------------------------------------

            SubscriptionStatus? selectedStatus = null;

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<SubscriptionStatus>(
                    status,
                    true,
                    out var parsedStatus))
                {
                    selectedStatus = parsedStatus;
                }
            }

            // ----------------------------------------------------
            // BASE FILTER
            // ----------------------------------------------------

            IEnumerable<Subscription> filteredSubscriptions =
                subscriptions;

            // Restaurant filter
            if (restaurantId.HasValue)
            {
                filteredSubscriptions = filteredSubscriptions
                    .Where(s => s.RestaurantId == restaurantId.Value);
            }

            // Search restaurant OR plan
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchText = search.Trim();

                filteredSubscriptions = filteredSubscriptions
                    .Where(s =>
                        (s.Restaurant != null &&
                         s.Restaurant.Name.Contains(
                             searchText,
                             StringComparison.OrdinalIgnoreCase))
                        ||
                        (s.SubscriptionPlan != null &&
                         s.SubscriptionPlan.Name.Contains(
                             searchText,
                             StringComparison.OrdinalIgnoreCase)));
            }

            // Status filter
            if (selectedStatus.HasValue)
            {
                filteredSubscriptions = filteredSubscriptions
                    .Where(s => s.Status == selectedStatus.Value);
            }

            // Date filter
            if (filterFrom.HasValue && filterTo.HasValue)
            {
                filteredSubscriptions = filteredSubscriptions
                    .Where(s =>
                        s.CreatedAt >= filterFrom.Value &&
                        s.CreatedAt <= filterTo.Value);
            }

            var filteredList = filteredSubscriptions.ToList();

            // ----------------------------------------------------
            // REVENUE
            // ----------------------------------------------------

            var paidSubscriptions = filteredList
                .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                .ToList();

            // Today's actual paid revenue
            var todayRevenue = subscriptions
                .Where(s =>
                    s.PaymentStatus == PaymentStatus.Paid &&
                    s.PaidAt.HasValue &&
                    s.PaidAt.Value >= today &&
                    s.PaidAt.Value < today.AddDays(1))
                .Sum(s => s.Amount);

            // This month's actual paid revenue
            var thisMonthRevenue = subscriptions
                .Where(s =>
                    s.PaymentStatus == PaymentStatus.Paid &&
                    s.PaidAt.HasValue &&
                    s.PaidAt.Value >= monthStart &&
                    s.PaidAt.Value < nextMonthStart)
                .Sum(s => s.Amount);

            // Total paid revenue
            var totalRevenue = subscriptions
                .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                .Sum(s => s.Amount);

            // ----------------------------------------------------
            // MODEL
            // ----------------------------------------------------

            var model = new SubscriptionReportViewModel
            {
                TotalRestaurants = restaurants.Count,

                TotalSubscriptions = filteredList.Count,

                ActiveSubscriptions = filteredList.Count(
                    s => s.Status == SubscriptionStatus.Active),

                ExpiredSubscriptions = filteredList.Count(
                    s => s.Status == SubscriptionStatus.Expired),

                PendingSubscriptions = filteredList.Count(
                    s => s.Status == SubscriptionStatus.Pending),

                TotalRevenue = paidSubscriptions.Sum(s => s.Amount)
            };

            // ----------------------------------------------------
            // RESTAURANT REPORT
            // ----------------------------------------------------

            foreach (var restaurant in restaurants)
            {
                var restaurantSubscriptions = filteredList
                    .Where(s => s.RestaurantId == restaurant.Id)
                    .ToList();

                // If filters are being used and this restaurant has
                // no matching subscriptions, don't show it.
                if (
                    (restaurantId.HasValue ||
                     !string.IsNullOrWhiteSpace(search) ||
                     selectedStatus.HasValue ||
                     filterFrom.HasValue)
                    &&
                    !restaurantSubscriptions.Any())
                {
                    continue;
                }

                // IMPORTANT:
                // Current subscription is taken from ALL subscriptions,
                // not date-filtered subscriptions.
                var currentSubscription = subscriptions
                    .Where(s =>
                        s.RestaurantId == restaurant.Id &&
                        s.Status == SubscriptionStatus.Active &&
                        s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefault();

                var owner = users
                    .Where(u => u.RestaurantId == restaurant.Id)
                    .OrderBy(u => u.CreatedAt)
                    .FirstOrDefault();

                var paidRestaurantSubscriptions =
                    restaurantSubscriptions
                        .Where(s =>
                            s.PaymentStatus == PaymentStatus.Paid)
                        .ToList();

                model.Restaurants.Add(
                    new RestaurantSubscriptionReportItem
                    {
                        RestaurantId = restaurant.Id,

                        RestaurantName = restaurant.Name,

                        OwnerName = owner?.FullName,

                        OwnerEmail = owner?.Email,

                        TotalSubscriptions =
                            restaurantSubscriptions.Count,

                        ActiveSubscriptions =
                            restaurantSubscriptions.Count(
                                s => s.Status ==
                                     SubscriptionStatus.Active),

                        ExpiredSubscriptions =
                            restaurantSubscriptions.Count(
                                s => s.Status ==
                                     SubscriptionStatus.Expired),

                        TotalAmount =
                            paidRestaurantSubscriptions
                                .Sum(s => s.Amount),

                        CurrentPlan =
                            currentSubscription?
                                .SubscriptionPlan?.Name,

                        CurrentStartDate =
                            currentSubscription?.StartDate,

                        CurrentEndDate =
                            currentSubscription?.EndDate,

                        CurrentStatus =
                            currentSubscription?.Status
                    });
            }

            // Highest revenue first
            model.Restaurants = model.Restaurants
                .OrderByDescending(r => r.TotalAmount)
                .ThenBy(r => r.RestaurantName)
                .ToList();

            // ----------------------------------------------------
            // VIEWBAGS
            // ----------------------------------------------------

            ViewBag.Restaurants = restaurants;

            ViewBag.SelectedRestaurantId = restaurantId;

            ViewBag.Search = search;

            ViewBag.Status = status ?? "all";

            ViewBag.DateRange = dateRange ?? "all";

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            ViewBag.TodayRevenue = todayRevenue;

            ViewBag.ThisMonthRevenue = thisMonthRevenue;

            ViewBag.TotalRevenue = totalRevenue;

            ViewBag.TodaySubscriptions =
                subscriptions.Count(s =>
                    s.CreatedAt >= today &&
                    s.CreatedAt < today.AddDays(1));

            ViewBag.ThisMonthSubscriptions =
                subscriptions.Count(s =>
                    s.CreatedAt >= monthStart &&
                    s.CreatedAt < nextMonthStart);

            ViewBag.PaidPayments =
                filteredList.Count(s =>
                    s.PaymentStatus == PaymentStatus.Paid);

            ViewBag.PendingPayments =
                filteredList.Count(s =>
                    s.PaymentStatus == PaymentStatus.Pending);

            ViewBag.FailedPayments =
                filteredList.Count(s =>
                    s.PaymentStatus == PaymentStatus.Failed);

            ViewBag.RefundedPayments =
                filteredList.Count(s =>
                    s.PaymentStatus == PaymentStatus.Refunded);

            ViewBag.CancelledPayments =
                filteredList.Count(s =>
                    s.PaymentStatus == PaymentStatus.Cancelled);

            // Payment methods
            ViewBag.RazorpayPayments =
                paidSubscriptions.Count(s =>
                    s.PaymentMethod == PaymentMethod.Razorpay);

            ViewBag.UPIPayments =
                paidSubscriptions.Count(s =>
                    s.PaymentMethod == PaymentMethod.UPI);

            ViewBag.CashPayments =
                paidSubscriptions.Count(s =>
                    s.PaymentMethod == PaymentMethod.Cash);

            ViewBag.ManualPayments =
                paidSubscriptions.Count(s =>
                    s.PaymentMethod == PaymentMethod.Manual);

            // Expiring subscriptions
            var sevenDaysFromNow = now.AddDays(7);
            var thirtyDaysFromNow = now.AddDays(30);

            ViewBag.ExpiringIn7Days =
                subscriptions.Count(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate >= now &&
                    s.EndDate <= sevenDaysFromNow);

            ViewBag.ExpiringIn30Days =
                subscriptions.Count(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate >= now &&
                    s.EndDate <= thirtyDaysFromNow);

            // ----------------------------------------------------
            // PLAN-WISE REVENUE
            // ----------------------------------------------------

            ViewBag.PlanRevenue = paidSubscriptions
                .GroupBy(s => s.SubscriptionPlan?.Name ?? "Unknown Plan")
                .Select(g => new
                {
                    Plan = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // ----------------------------------------------------
            // RESTAURANT-WISE PAID REVENUE
            // ----------------------------------------------------

            ViewBag.RestaurantRevenue = paidSubscriptions
                .GroupBy(s => new
                {
                    s.RestaurantId,
                    RestaurantName =
                        s.Restaurant?.Name ?? "Unknown Restaurant"
                })
                .Select(g => new
                {
                    RestaurantId = g.Key.RestaurantId,
                    RestaurantName = g.Key.RestaurantName,
                    PaidSubscriptions = g.Count(),
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            return View(model);
        }
    }
}