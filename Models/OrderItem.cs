using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantQR.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        // Original menu item
        public int MenuItemId { get; set; }

        public MenuItem MenuItem { get; set; } = null!;



        // =====================================================
        // SERVING OPTION
        // =====================================================

        public int? MenuItemOptionId { get; set; }

        public MenuItemOption? MenuItemOption { get; set; }


        // =====================================================
        // SNAPSHOT
        // =====================================================

        [Required]
        [MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;


        [MaxLength(100)]
        public string? OptionName { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }


        public int Quantity { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }
}