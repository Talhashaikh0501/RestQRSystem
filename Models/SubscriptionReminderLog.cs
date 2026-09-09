using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.Models
{
    public class SubscriptionReminderLog
    {
        public int Id { get; set; }

        [Required]
        public int SubscriptionId { get; set; }

        // Example:
        // 7 = 7 days remaining reminder
        // 6 = 6 days remaining reminder
        // ...
        // 1 = 1 day remaining reminder
        // 0 = subscription expired email
        public int DaysRemaining { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReminderType { get; set; }
            = "ExpiryReminder";

        public DateTime SentAt { get; set; }
            = DateTime.UtcNow;


        // Navigation
        public Subscription Subscription { get; set; }
            = null!;
    }
}