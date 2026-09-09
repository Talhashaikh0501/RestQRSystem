namespace RestaurantQR.Services
{
    public interface IEmailService
    {
        Task SendPaymentReceivedEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            string adminEmail,
            string temporaryPassword
        );

        Task SendSubscriptionApprovedEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            string adminEmail
        );

        Task SendSubscriptionDeactivatedEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName
        );

        Task SendSubscriptionExpiryReminderEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            int daysRemaining,
            DateTime expiryDate,
            decimal renewalAmount
        );

        Task SendSubscriptionExpiredEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            DateTime expiryDate
        );
    }
}