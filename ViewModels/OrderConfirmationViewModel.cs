namespace RestaurantQR.ViewModels
{
    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string TableNumber { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public string Status { get; set; } = string.Empty;

        public string TrackingToken { get; set; } = string.Empty;
    }
}