using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RestaurantTable> Tables { get; set; }
            = new List<RestaurantTable>();

        public ICollection<Category> Categories { get; set; }
            = new List<Category>();

        public ICollection<ApplicationUser> Users { get; set; }
            = new List<ApplicationUser>();
        public ICollection<Subscription> Subscriptions { get; set; }
    = new List<Subscription>();
    }
}