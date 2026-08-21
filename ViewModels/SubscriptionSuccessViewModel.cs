using RestaurantQR.Models;

namespace RestaurantQR.ViewModels
{
    public class SubscriptionSuccessViewModel
    {
        public Subscription Subscription { get; set; } = null!;

        public string AdminEmail { get; set; } = string.Empty;

        public string TemporaryPassword { get; set; } = string.Empty;
    }
}