using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RestaurantQR.ViewModels
{
    public class MenuItemViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Food Image")]
        public IFormFile? Image { get; set; }

        public string? ExistingImageUrl { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
            = Enumerable.Empty<SelectListItem>();
    }
}