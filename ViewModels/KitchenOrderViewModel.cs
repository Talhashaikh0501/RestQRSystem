namespace RestaurantQR.ViewModels
{
    public class KitchenOrderViewModel
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string TableNumber { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? CustomerNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<KitchenOrderItemViewModel> Items { get; set; }
            = new();
    }

    public class KitchenOrderItemViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string? OptionName { get; set; }

        public int Quantity { get; set; }
    }
}