using System.ComponentModel.DataAnnotations;

namespace RestaurantQR.ViewModels
{
    public class RestaurantTableViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Table number is required.")]
        [MaxLength(50)]
        [Display(Name = "Table Number")]
        public string TableNumber { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
        public int Capacity { get; set; } = 2;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}