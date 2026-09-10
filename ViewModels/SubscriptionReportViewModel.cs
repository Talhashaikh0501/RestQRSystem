using RestaurantQR.Models;

namespace RestaurantQR.ViewModels
{
    public class SubscriptionReportViewModel
    {
        public int TotalRestaurants { get; set; }

        public int TotalSubscriptions { get; set; }

        public int ActiveSubscriptions { get; set; }

        public int ExpiredSubscriptions { get; set; }

        public int PendingSubscriptions { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<RestaurantSubscriptionReportItem> Restaurants { get; set; }
            = new();
    }

    public class RestaurantSubscriptionReportItem
    {
        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; }
            = string.Empty;

        public string? OwnerName { get; set; }

        public string? OwnerEmail { get; set; }

        public int TotalSubscriptions { get; set; }

        public int ActiveSubscriptions { get; set; }

        public int ExpiredSubscriptions { get; set; }

        public decimal TotalAmount { get; set; }

        public string? CurrentPlan { get; set; }

        public DateTime? CurrentStartDate { get; set; }

        public DateTime? CurrentEndDate { get; set; }

        public SubscriptionStatus? CurrentStatus { get; set; }
    }
}