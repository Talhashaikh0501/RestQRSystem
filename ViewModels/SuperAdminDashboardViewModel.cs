namespace RestaurantQR.ViewModels
{
    public class SuperAdminDashboardViewModel
    {
        public DateTime GeneratedAtUtc { get; set; }

        // =========================================================
        // REVENUE RANGE
        // =========================================================

        public string SelectedRevenueRange { get; set; } = "30d";

        public string SelectedRevenueRangeLabel { get; set; }
            = "Last 30 Days";

        public decimal SelectedPaidSubscriptionRevenue { get; set; }

        public decimal TotalPaidSubscriptionRevenue { get; set; }


        // =========================================================
        // GRAPH DATA
        // =========================================================

        public List<SuperAdminDashboardPoint> SubscriptionRevenueTrend
        {
            get;
            set;
        } = new();


        public List<SuperAdminDashboardPoint> RestaurantGrowthTrend
        {
            get;
            set;
        } = new();


        public List<SuperAdminDashboardPoint> RestaurantStatusDistribution
        {
            get;
            set;
        } = new();


        public List<SuperAdminDashboardPoint> SubscriptionStatusDistribution
        {
            get;
            set;
        } = new();


        public List<SuperAdminDashboardPoint> SubscriptionPlanDistribution
        {
            get;
            set;
        } = new();


        public List<SuperAdminDashboardPoint> SubscriptionExpiryTrend
        {
            get;
            set;
        } = new();


        public List<SuperAdminDashboardPoint> PlatformUsageTrend
        {
            get;
            set;
        } = new();
    }


    public class SuperAdminDashboardPoint
    {
        public string Label { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }
}