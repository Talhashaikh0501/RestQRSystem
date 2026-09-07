using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class CustomerDetailsViewModel
    {
        public int RestaurantId { get; set; }

        public int TableId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public string TableNumber { get; set; } = string.Empty;

        [Required]
        public string QRToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(
            80,
            MinimumLength = 2,
            ErrorMessage = "Name must be between 2 and 80 characters.")]
        [Display(Name = "Your name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your phone number.")]
        [StringLength(
            24,
            ErrorMessage = "Phone number is too long.")]
        [Display(Name = "Phone number")]
        public string CustomerPhone { get; set; } = string.Empty;
    }
}