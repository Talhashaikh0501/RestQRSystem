namespace RestaurantQR.ViewModels
{
    public class QRMenuViewModel
    {
        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public int TableId { get; set; }

        public string TableNumber { get; set; } = string.Empty;

        public string QRToken { get; set; } = string.Empty;

        // Customer currently ordering
        public string CustomerName { get; set; } = string.Empty;

        // Used by the sticky cart on the menu page
        public int CartQuantity { get; set; }

        public decimal CartTotal { get; set; }

        public List<QRMenuCategoryViewModel> Categories { get; set; }
            = new();
    }

    public class QRMenuCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;

        public List<QRMenuItemViewModel> MenuItems { get; set; }
            = new();
    }

    public class QRMenuItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public List<QRMenuItemOptionViewModel> Options { get; set; }
            = new();
    }

    public class QRMenuItemOptionViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsAvailable { get; set; }
    }
}