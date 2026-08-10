namespace RestaurantQR.ViewModels
{
    public class AdminOrderDetailsViewModel
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string TableNumber { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal Tax { get; set; }

        public decimal Total { get; set; }

        public string? CustomerNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<AdminOrderItemViewModel> Items { get; set; }
            = new();
    }

    public class AdminOrderItemViewModel
    {
        public string Name { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }
}