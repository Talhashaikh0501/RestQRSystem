namespace RestaurantQR.ViewModels
{
    public class CartViewModel
    {
        public int RestaurantId { get; set; }

        public int TableId { get; set; }

        public string TableNumber { get; set; } = string.Empty;

        public string QRToken { get; set; } = string.Empty;

        public List<CartItemViewModel> Items { get; set; }
            = new();

        public decimal Subtotal =>
            Items.Sum(i => i.LineTotal);

        public int TotalQuantity =>
            Items.Sum(i => i.Quantity);
    }
}