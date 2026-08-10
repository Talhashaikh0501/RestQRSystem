using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class CreateKitchenUserViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}