using RestaurantQR.Models;
using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Foreign Key
        public int RestaurantId { get; set; }

        // Navigation Property
        public Restaurant Restaurant { get; set; } = null!;

        public ICollection<MenuItem> MenuItems { get; set; }
            = new List<MenuItem>();
    }
}