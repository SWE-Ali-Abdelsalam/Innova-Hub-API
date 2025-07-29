using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class CreateProductFromDealDTO
    {
        public int DealId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 100 characters.")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(8000, ErrorMessage = "Description cannot exceed 8000 characters.")]
        public string ProductDescription { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number.")]
        public int? CategoryId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number.")]
        public int? Stock { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        public decimal? Discount { get; set; }

        [Required(ErrorMessage = "Product dimensions are required.")]
        [RegularExpression(@"^\d+(\.\d+)?\*\d+(\.\d+)?\*\d+(\.\d+)?$",
        ErrorMessage = "Dimensions must be in 'Height*Width*Depth' format, e.g., '50.5*60.2*10.75'.")]
        public string Dimensions { get; set; }

        [Required(ErrorMessage = "Product weight is required.")]
        [Range(0.1, double.MaxValue, ErrorMessage = "Weight must be greater than zero.")]
        public double Weight { get; set; }
    }
}
