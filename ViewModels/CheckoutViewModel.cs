using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();

        [MaxLength(1000)]
        [Display(Name = "Order Notes")]
        public string? CustomerNote { get; set; }
    }
}