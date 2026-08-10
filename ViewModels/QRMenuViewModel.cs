namespace RestaurantQR.ViewModels
{
    public class QRMenuViewModel
    {
        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public int TableId { get; set; }

        public string TableNumber { get; set; } = string.Empty;

        public string QRToken { get; set; } = string.Empty;

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
    }
}