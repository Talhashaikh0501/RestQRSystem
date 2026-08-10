namespace RestaurantQR.ViewModels
{
    public class RestaurantAdminListViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public bool RestaurantIsActive { get; set; }
    }
}