using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class ResetAdminPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? AdminEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}