using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class MenuItemOptionViewModel
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 999999)]
        public decimal Price { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}