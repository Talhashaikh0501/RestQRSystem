using RestaurantQR.Models;
using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.Models
{
    public class RestaurantTable
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TableNumber { get; set; } = string.Empty;

        public int Capacity { get; set; }

        // Used in the QR URL so customers don't need the database ID.
        [Required]
        [MaxLength(100)]
        public string QRToken { get; set; } = Guid.NewGuid().ToString("N");

        public bool IsActive { get; set; } = true;

        // Foreign Key
        public int RestaurantId { get; set; }

        // Navigation Property
        public Restaurant Restaurant { get; set; } = null!;
    }
}