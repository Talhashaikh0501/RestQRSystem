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

        public async Task<IActionResult> Index(int? restaurantId)
        {
            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();

            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Restaurant)
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.RestaurantId != null)
                .ToListAsync();

            // ---------------------------------------------------------
            // Overall statistics
            // ---------------------------------------------------------

            var model = new SubscriptionReportViewModel
            {
                TotalRestaurants = restaurants.Count,

                TotalSubscriptions = subscriptions.Count,

                ActiveSubscriptions = subscriptions.Count(
                    s => s.Status == SubscriptionStatus.Active),

                ExpiredSubscriptions = subscriptions.Count(
                    s => s.Status == SubscriptionStatus.Expired),

                PendingSubscriptions = subscriptions.Count(
                    s => s.Status == SubscriptionStatus.Pending),

                TotalRevenue = subscriptions
                    .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                    .Sum(s => s.Amount)
            };

            // ---------------------------------------------------------
            // Restaurant-wise report
            // ---------------------------------------------------------

            foreach (var restaurant in restaurants)
            {
                var restaurantSubscriptions = subscriptions
                    .Where(s => s.RestaurantId == restaurant.Id)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToList();

                var owner = users
                    .FirstOrDefault(u => u.RestaurantId == restaurant.Id);

                var currentSubscription = restaurantSubscriptions
                    .Where(s =>
                        s.Status == SubscriptionStatus.Active &&
                        s.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefault();

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
                                s => s.Status == SubscriptionStatus.Active),

                        ExpiredSubscriptions =
                            restaurantSubscriptions.Count(
                                s => s.Status == SubscriptionStatus.Expired),

                        TotalAmount =
                            restaurantSubscriptions
                                .Where(s =>
                                    s.PaymentStatus == PaymentStatus.Paid)
                                .Sum(s => s.Amount),

                        CurrentPlan =
                            currentSubscription?.SubscriptionPlan?.Name,

                        CurrentStartDate =
                            currentSubscription?.StartDate,

                        CurrentEndDate =
                            currentSubscription?.EndDate,

                        CurrentStatus =
                            currentSubscription?.Status
                    });
            }

            // ---------------------------------------------------------
            // Optional restaurant filter
            // ---------------------------------------------------------

            if (restaurantId.HasValue)
            {
                model.Restaurants = model.Restaurants
                    .Where(r => r.RestaurantId == restaurantId.Value)
                    .ToList();
            }

            ViewBag.Restaurants = restaurants;

            ViewBag.SelectedRestaurantId = restaurantId;

            return View(model);
        }
    }
}