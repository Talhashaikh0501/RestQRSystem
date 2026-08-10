using Microsoft.AspNetCore.Identity;

namespace RestaurantQR.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NULL for SuperAdmin.
        // RestaurantAdmin/Kitchen users will have a RestaurantId.
        public int? RestaurantId { get; set; }

        public Restaurant? Restaurant { get; set; }
    }
}