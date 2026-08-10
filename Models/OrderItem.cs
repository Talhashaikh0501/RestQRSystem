using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantQR.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        // Keep the menu item relationship for reference.
        public int MenuItemId { get; set; }

        public MenuItem MenuItem { get; set; } = null!;

        // Snapshot values preserve the original order history.
        [Required]
        [MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }
}