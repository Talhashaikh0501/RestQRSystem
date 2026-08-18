using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class AssignSubscriptionViewModel
    {
        [Required]
        public int RestaurantId { get; set; }

        [Required(ErrorMessage = "Please select a subscription plan.")]
        public int SubscriptionPlanId { get; set; }

        [Required(ErrorMessage = "Please select a start date.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
            = DateTime.UtcNow.Date;
    }
}