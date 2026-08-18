namespace RestaurantQR.ViewModels
{
    public class CartItemViewModel
    {
        public int MenuItemId { get; set; }

        public int OptionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string OptionName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string? ImageUrl { get; set; }

        public decimal LineTotal =>
            Price * Quantity;
    }
}