using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UpdateProductDTO
    {
        [Required(ErrorMessage = "Product ID is required.")]
        public int ProductId { get; set; }

        public string? ProductName { get; set; } = string.Empty;

        public IFormFile? Homepicture { get; set; } // ✅ Optional for updates

        public List<IFormFile>? Pictures { get; set; } // ✅ Optional for updates

        public string? Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal? Price { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        public decimal? Discount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number.")]
        public int? CategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        public int? Stock { get; set; }

        public string? Dimensions { get; set; } // ✅ Optional for updates

        public double? Weight { get; set; } // ✅ Optional for updates

        public List<string>? SizeNames { get; set; } // ✅ Optional for updates
        public List<string>? ColorNames { get; set; } // ✅ Optional for updates
    }

}
