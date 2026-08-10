using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantQR.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string TrackingToken { get; set; } =
    Guid.NewGuid().ToString("N");

        [Required]
        [MaxLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; } = null!;

        public int RestaurantTableId { get; set; }

        public RestaurantTable RestaurantTable { get; set; } = null!;

        // Identifies the customer's temporary browser/session order.
        [Required]
        [MaxLength(100)]
        public string CustomerSessionId { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [MaxLength(1000)]
        public string? CustomerNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();
    }
}