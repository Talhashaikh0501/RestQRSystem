using RestaurantQR.Models;
using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class PurchaseSubscriptionViewModel
    {
        // Selected Subscription Plan
        public int SubscriptionPlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public int DurationDays { get; set; }


        // Restaurant Details

        [Required(ErrorMessage = "Restaurant name is required.")]
        [MaxLength(150)]
        public string RestaurantName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Owner name is required.")]
        [MaxLength(100)]
        public string OwnerName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;


        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;


        // Subscription Start Date

        [Required(ErrorMessage = "Please select a start date.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;


        // Payment Method
        public PaymentMethod? PaymentMethod { get; set; }
    }
}