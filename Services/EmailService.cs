using System.Net;
using System.Net.Mail;

namespace RestaurantQR.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }


        // =========================================================
        // 1. PAYMENT RECEIVED
        // =========================================================

        public async Task SendPaymentReceivedEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            string adminEmail,
            string temporaryPassword)
        {
            var subject =
                "Payment Received - RestaurantQR Approval Pending";

            var body = BuildEmailTemplate(
                title: "Payment Received Successfully",
                subtitle: "Your account is waiting for Super Admin approval.",
                content: $@"
                    <p>Hello <strong>{E(ownerName)}</strong>,</p>

                    <p>
                        We have successfully received the payment for
                        <strong>{E(restaurantName)}</strong>.
                    </p>

                    <div style=""background:#fff4dc;border:1px solid #e3c481;padding:18px;border-radius:14px;margin:20px 0;"">
                        <strong style=""color:#8a632b;"">
                            Super Admin Approval Pending
                        </strong>

                        <p style=""margin:8px 0 0;"">
                            Super Admin will confirm your request after
                            payment verification. You will receive another
                            email when your restaurant account is activated.
                        </p>
                    </div>

                    <p><strong>Plan:</strong> {E(planName)}</p>

                    <div style=""background:#352822;color:white;padding:20px;border-radius:14px;margin-top:20px;"">
                        <p style=""margin-top:0;color:#dccbbd;"">
                            TEMPORARY LOGIN CREDENTIALS
                        </p>

                        <p>
                            <strong>Email:</strong><br>
                            {E(adminEmail)}
                        </p>

                        <p style=""margin-bottom:0;"">
                            <strong>Temporary Password:</strong><br>
                            <span style=""font-size:20px;color:#f5e6ca;"">
                                {E(temporaryPassword)}
                            </span>
                        </p>
                    </div>

                    <p>
                        Please keep these credentials secure.
                    </p>"
            );

            await SendEmailAsync(
                recipientEmail,
                subject,
                body);
        }


        // =========================================================
        // 2. SUBSCRIPTION APPROVED
        // =========================================================

        public async Task SendSubscriptionApprovedEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            string adminEmail)
        {
            var subject =
                "RestaurantQR Account Approved";

            var body = BuildEmailTemplate(
                title: "Your Account Is Active",
                subtitle: "Super Admin has approved your subscription.",
                content: $@"
                    <p>Hello <strong>{E(ownerName)}</strong>,</p>

                    <p>
                        Your subscription for
                        <strong>{E(restaurantName)}</strong>
                        has been approved successfully.
                    </p>

                    <div style=""background:#edf6e8;border:1px solid #bfd0ae;padding:18px;border-radius:14px;margin:20px 0;"">
                        <strong style=""color:#597143;"">
                            Account Activated
                        </strong>

                        <p style=""margin:8px 0 0;"">
                            Your RestaurantQR account is now active
                            and ready to use.
                        </p>
                    </div>

                    <p>
                        <strong>Plan:</strong> {E(planName)}
                    </p>

                    <p>
                        <strong>Admin Email:</strong> {E(adminEmail)}
                    </p>

                    <p>
                        You can now log in using the credentials
                        previously sent to you.
                    </p>"
            );

            await SendEmailAsync(
                recipientEmail,
                subject,
                body);
        }


        // =========================================================
        // 3. MANUAL DEACTIVATION
        // =========================================================

        public async Task SendSubscriptionDeactivatedEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName)
        {
            var subject =
                "RestaurantQR Account Deactivated";

            var body = BuildEmailTemplate(
                title: "Your Account Has Been Deactivated",
                subtitle: "Restaurant access is currently unavailable.",
                content: $@"
                    <p>Hello <strong>{E(ownerName)}</strong>,</p>

                    <p>
                        The RestaurantQR account for
                        <strong>{E(restaurantName)}</strong>
                        has been deactivated by Super Admin.
                    </p>

                    <div style=""background:#fbeceb;border:1px solid #e5b6b1;padding:18px;border-radius:14px;margin:20px 0;"">
                        <strong style=""color:#954d45;"">
                            Account Inactive
                        </strong>

                        <p style=""margin:8px 0 0;"">
                            You will not be able to use the RestaurantQR
                            Admin system while the account is inactive.
                        </p>
                    </div>

                    <p>
                        Please contact the RestaurantQR administrator
                        or complete any required subscription payment
                        if applicable.
                    </p>"
            );

            await SendEmailAsync(
                recipientEmail,
                subject,
                body);
        }


        // =========================================================
        // 4. DAILY EXPIRY REMINDER - 7 TO 1 DAYS
        // =========================================================

        public async Task SendSubscriptionExpiryReminderEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            int daysRemaining,
            DateTime expiryDate,
            decimal renewalAmount)
        {
            var dayText =
                daysRemaining == 1
                    ? "1 Day"
                    : $"{daysRemaining} Days";

            var urgency =
                daysRemaining <= 2
                    ? "URGENT"
                    : "Subscription Reminder";

            var subject =
                $"{urgency} - Only {dayText} Remaining";

            var expiryText =
                expiryDate.ToString("dd MMMM yyyy");

            var body = BuildEmailTemplate(
                title: $"Only {dayText} Remaining",
                subtitle: "Renew your RestaurantQR subscription before it expires.",
                content: $@"
                    <p>Hello <strong>{E(ownerName)}</strong>,</p>

                    <p>
                        Your RestaurantQR subscription for
                        <strong>{E(restaurantName)}</strong>
                        will expire in
                        <strong>{dayText}</strong>.
                    </p>

                    <div style=""background:#fff3d7;border:1px solid #e7c36d;padding:20px;border-radius:14px;margin:20px 0;"">

                        <div style=""font-size:22px;font-weight:bold;color:#9a6923;"">
                            {dayText} Remaining
                        </div>

                        <p style=""margin-bottom:0;"">
                            Please complete your renewal payment as soon
                            as possible to avoid interruption.
                        </p>

                    </div>

                    <table width=""100%"" cellpadding=""9""
                           style=""background:#faf6ef;border-radius:12px;"">

                        <tr>
                            <td>Restaurant</td>
                            <td align=""right"">
                                <strong>{E(restaurantName)}</strong>
                            </td>
                        </tr>

                        <tr>
                            <td>Plan</td>
                            <td align=""right"">
                                <strong>{E(planName)}</strong>
                            </td>
                        </tr>

                        <tr>
                            <td>Expiry Date</td>
                            <td align=""right"">
                                <strong>{E(expiryText)}</strong>
                            </td>
                        </tr>

                        <tr>
                            <td>Renewal Amount</td>
                            <td align=""right"">
                                <strong>₹{renewalAmount:N2}</strong>
                            </td>
                        </tr>

                    </table>

                    <div style=""background:#fbeceb;border:1px solid #e5b6b1;padding:18px;border-radius:14px;margin-top:20px;color:#864c45;"">

                        <strong>
                            Important
                        </strong>

                        <p style=""margin:8px 0 0;"">
                            If your subscription is not renewed before
                            expiry, your RestaurantQR account will be
                            automatically disabled and you will no longer
                            be able to use the Restaurant Admin system.
                        </p>

                    </div>

                    <p>
                        Please renew your subscription immediately
                        to keep your restaurant running without interruption.
                    </p>"
            );

            await SendEmailAsync(
                recipientEmail,
                subject,
                body);
        }


        // =========================================================
        // 5. SUBSCRIPTION EXPIRED
        // =========================================================

        public async Task SendSubscriptionExpiredEmailAsync(
            string recipientEmail,
            string ownerName,
            string restaurantName,
            string planName,
            DateTime expiryDate)
        {
            var expiryText =
                expiryDate.ToString("dd MMMM yyyy");

            var subject =
                "RestaurantQR Subscription Expired - Account Disabled";

            var body = BuildEmailTemplate(
                title: "Subscription Expired",
                subtitle: "Your RestaurantQR account has been disabled.",
                content: $@"
                    <p>Hello <strong>{E(ownerName)}</strong>,</p>

                    <p>
                        The subscription for
                        <strong>{E(restaurantName)}</strong>
                        has expired.
                    </p>

                    <div style=""background:#fbeceb;border:1px solid #e2ada8;padding:20px;border-radius:14px;margin:20px 0;"">

                        <div style=""font-size:21px;font-weight:bold;color:#914940;"">
                            Account Disabled
                        </div>

                        <p style=""margin-bottom:0;"">
                            Your restaurant account can no longer access
                            RestaurantQR until the subscription is renewed.
                        </p>

                    </div>

                    <p>
                        <strong>Plan:</strong>
                        {E(planName)}
                    </p>

                    <p>
                        <strong>Expired On:</strong>
                        {E(expiryText)}
                    </p>

                    <p>
                        Please complete your renewal payment to restore
                        RestaurantQR access.
                    </p>"
            );

            await SendEmailAsync(
                recipientEmail,
                subject,
                body);
        }


        // =========================================================
        // COMMON HTML TEMPLATE
        // =========================================================

        private static string BuildEmailTemplate(
            string title,
            string subtitle,
            string content)
        {
            return $@"
<!DOCTYPE html>

<html>

<head>

    <meta charset=""UTF-8"">

    <meta name=""viewport""
          content=""width=device-width, initial-scale=1.0"">

</head>


<body style=""
    margin:0;
    padding:0;
    background:#f5efe6;
    font-family:Arial,Helvetica,sans-serif;
    color:#4b3832;
"">


<table width=""100%""
       cellpadding=""0""
       cellspacing=""0""
       border=""0""
       style=""background:#f5efe6;padding:35px 15px;"">


<tr>

<td align=""center"">


<table width=""100%""
       cellpadding=""0""
       cellspacing=""0""
       border=""0""
       style=""
           max-width:620px;
           background:#fffdf8;
           border-radius:22px;
           overflow:hidden;
           box-shadow:0 12px 35px rgba(70,45,30,.12);
       "">


<tr>

<td style=""
    background:#352822;
    padding:30px;
    text-align:center;
"">

    <div style=""
        font-size:27px;
        font-weight:bold;
        color:#f5e6ca;
    "">

        RestaurantQR

    </div>


    <div style=""
        margin-top:7px;
        color:#d6c1a5;
        font-size:12px;
        letter-spacing:1px;
    "">

        SMART DINING PLATFORM

    </div>

</td>

</tr>


<tr>

<td style=""padding:35px;"">


    <h1 style=""
        margin:0 0 8px;
        color:#4b3832;
        font-size:25px;
    "">

        {title}

    </h1>


    <p style=""
        margin:0 0 25px;
        color:#9a806f;
        font-size:14px;
    "">

        {subtitle}

    </p>


    <div style=""
        color:#6f5a4c;
        font-size:14px;
        line-height:1.7;
    "">

        {content}

    </div>


</td>

</tr>


<tr>

<td style=""
    padding:20px 35px 30px;
    text-align:center;
    color:#9b8879;
    font-size:11px;
"">

    This is an automated email from RestaurantQR.

</td>

</tr>


</table>


</td>

</tr>


</table>


</body>

</html>";
        }


        // =========================================================
        // HTML ENCODING
        // =========================================================

        private static string E(string? value)
        {
            return WebUtility.HtmlEncode(
                value ?? string.Empty);
        }


        // =========================================================
        // SMTP SENDER
        // =========================================================

        private async Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody)
        {
            try
            {
                var host =
                    _configuration[
                        "EmailSettings:SmtpHost"
                    ];

                var portText =
                    _configuration[
                        "EmailSettings:SmtpPort"
                    ];

                var senderEmail =
                    _configuration[
                        "EmailSettings:SenderEmail"
                    ];

                var senderName =
                    _configuration[
                        "EmailSettings:SenderName"
                    ];

                var username =
                    _configuration[
                        "EmailSettings:Username"
                    ];

                var password =
                    _configuration[
                        "EmailSettings:Password"
                    ];

                var sslText =
                    _configuration[
                        "EmailSettings:EnableSsl"
                    ];


                if (string.IsNullOrWhiteSpace(host))
                {
                    throw new InvalidOperationException(
                        "EmailSettings:SmtpHost is missing.");
                }


                if (!int.TryParse(
                    portText,
                    out var port))
                {
                    throw new InvalidOperationException(
                        "EmailSettings:SmtpPort is invalid.");
                }


                if (string.IsNullOrWhiteSpace(
                    senderEmail))
                {
                    throw new InvalidOperationException(
                        "EmailSettings:SenderEmail is missing.");
                }


                if (string.IsNullOrWhiteSpace(
                    username))
                {
                    username =
                        senderEmail;
                }


                if (string.IsNullOrWhiteSpace(
                    password))
                {
                    throw new InvalidOperationException(
                        "EmailSettings:Password is missing.");
                }


                var enableSsl =
                    !bool.TryParse(
                        sslText,
                        out var parsedSsl)
                    ||
                    parsedSsl;


                using var message =
                    new MailMessage();


                message.From =
                    new MailAddress(
                        senderEmail,
                        string.IsNullOrWhiteSpace(
                            senderName)
                            ? "RestaurantQR"
                            : senderName
                    );


                message.To.Add(
                    new MailAddress(
                        recipientEmail
                    )
                );


                message.Subject =
                    subject;

                message.Body =
                    htmlBody;

                message.IsBodyHtml =
                    true;


                using var smtp =
                    new SmtpClient(
                        host,
                        port
                    );


                smtp.EnableSsl =
                    enableSsl;

                smtp.UseDefaultCredentials =
                    false;

                smtp.Credentials =
                    new NetworkCredential(
                        username,
                        password
                    );


                await smtp.SendMailAsync(
                    message
                );


                _logger.LogInformation(
                    "RestaurantQR email sent successfully to {RecipientEmail}.",
                    recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RestaurantQR email failed for {RecipientEmail}.",
                    recipientEmail);

                throw;
            }
        }
    }
}