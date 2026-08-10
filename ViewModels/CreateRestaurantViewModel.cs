using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class CreateRestaurantViewModel
    {
        // --- Restaurant Details ---
        [Required(ErrorMessage = "Restaurant name is required")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        // --- Admin User Details ---
        [Required(ErrorMessage = "Admin full name is required")]
        public string AdminFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin email is required")]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string AdminPassword { get; set; } = string.Empty;
    }
}