using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int DurationDays { get; set; }

        [Required]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsCustom { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; }
            = new List<Subscription>();
    }
}