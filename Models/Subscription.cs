using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantQR.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required]
        public int SubscriptionPlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public SubscriptionStatus Status { get; set; }
            = SubscriptionStatus.Pending;

        public PaymentStatus PaymentStatus { get; set; }
            = PaymentStatus.Pending;

        public PaymentMethod? PaymentMethod { get; set; }

        [MaxLength(150)]
        public string? TransactionId { get; set; }

        [MaxLength(150)]
        public string? RazorpayOrderId { get; set; }

        [MaxLength(150)]
        public string? RazorpayPaymentId { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties

        public Restaurant Restaurant { get; set; }
            = null!;

        public SubscriptionPlan SubscriptionPlan { get; set; }
            = null!;
    }
}