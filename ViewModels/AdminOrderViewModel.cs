namespace RestaurantQR.ViewModels
{
    public class AdminOrderViewModel
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string TableNumber { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ItemCount { get; set; }
    }
}