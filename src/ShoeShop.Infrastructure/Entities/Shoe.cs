using System.ComponentModel.DataAnnotations;

namespace ShoeShop.Infrastructure.Entities
{
    public class Shoe
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Shoe name is required")]
        [StringLength(100, ErrorMessage = "Shoe name cannot exceed 100 characters")]
        [Display(Name = "Shoe Name")]
        public required string Name { get; set; }
        
        [Required(ErrorMessage = "Brand is required")]
        [StringLength(50, ErrorMessage = "Brand name cannot exceed 50 characters")]
        public required string Brand { get; set; }
        
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between $0.01 and $9999.99")]
        [Display(Name = "Price ($)")]
        public decimal Price { get; set; }
        
        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }
    }
}
