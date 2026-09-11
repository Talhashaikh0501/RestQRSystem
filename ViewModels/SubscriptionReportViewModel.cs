using RestaurantQR.Models;

namespace RestaurantQR.ViewModels
{
    public class SubscriptionReportViewModel
    {
        // =========================
        // MAIN SUMMARY
        // =========================

        public int TotalRestaurants { get; set; }

        public int FilteredRestaurants { get; set; }

        public int TotalSubscriptions { get; set; }

        public int ActiveSubscriptions { get; set; }

        public int ExpiredSubscriptions { get; set; }

        public int PendingSubscriptions { get; set; }

        public int CancelledSubscriptions { get; set; }

        public int SuspendedSubscriptions { get; set; }


        // =========================
        // REVENUE
        // =========================

        public decimal TotalRevenue { get; set; }

        public decimal TodayRevenue { get; set; }

        public decimal ThisMonthRevenue { get; set; }


        // =========================
        // SUBSCRIPTION PERIOD
        // =========================

        public int TodaySubscriptions { get; set; }

        public int ThisMonthSubscriptions { get; set; }


        // =========================
        // PAYMENT STATUS
        // =========================

        public int PaidPayments { get; set; }

        public int PendingPayments { get; set; }

        public int FailedPayments { get; set; }

        public int RefundedPayments { get; set; }

        public int CancelledPayments { get; set; }


        // =========================
        // PAYMENT METHODS
        // =========================

        public int RazorpayPayments { get; set; }

        public int UpiPayments { get; set; }

        public int CashPayments { get; set; }

        public int ManualPayments { get; set; }


        // =========================
        // EXPIRING SUBSCRIPTIONS
        // =========================

        public int ExpiringIn7Days { get; set; }

        public int ExpiringIn30Days { get; set; }


        // =========================
        // RESTAURANT REPORT
        // =========================

        public List<RestaurantSubscriptionReportItem> Restaurants { get; set; }
            = new();


        // =========================
        // PLAN REPORT
        // =========================

        public List<PlanRevenueReportItem> PlanRevenue { get; set; }
            = new();
    }


    // ============================================================
    // RESTAURANT REPORT ITEM
    // ============================================================

    public class RestaurantSubscriptionReportItem
    {
        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public string? OwnerName { get; set; }

        public string? OwnerEmail { get; set; }

        public int TotalSubscriptions { get; set; }

        public int ActiveSubscriptions { get; set; }

        public int ExpiredSubscriptions { get; set; }

        public int PendingSubscriptions { get; set; }

        public int CancelledSubscriptions { get; set; }

        public decimal TotalAmount { get; set; }

        public int PaidSubscriptions { get; set; }

        public string? CurrentPlan { get; set; }

        public DateTime? CurrentStartDate { get; set; }

        public DateTime? CurrentEndDate { get; set; }

        public SubscriptionStatus? CurrentStatus { get; set; }
    }


    // ============================================================
    // PLAN REVENUE REPORT
    // ============================================================

    public class PlanRevenueReportItem
    {
        public string PlanName { get; set; } = string.Empty;

        public int SubscriptionCount { get; set; }

        public decimal Revenue { get; set; }
    }
}