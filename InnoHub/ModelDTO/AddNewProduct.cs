using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class AddNewProduct//
    {
        [Required(ErrorMessage = "Product name is required.")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Home picture is required.")]
        [DataType(DataType.Upload)]
        public IFormFile HomePicture { get; set; }

        [Required(ErrorMessage = "At least one picture is required.")]
        [DataType(DataType.Upload)]
        public List<IFormFile> Pictures { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        public decimal Discount { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Stock is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number.")]
        public int Stock { get; set; }

        // ✅ Product Dimensions & Weight
        [Required(ErrorMessage = "Product dimensions are required.")]
        [RegularExpression(@"^\d+(\.\d+)?\*\d+(\.\d+)?\*\d+(\.\d+)?$",
        ErrorMessage = "Dimensions must be in 'Height*Width*Depth' format, e.g., '50.5*60.2*10.75'.")]
        public string Dimensions { get; set; }  // Example: "50.5*60.2*10.75"


        [Required(ErrorMessage = "Product weight is required.")]
        [Range(0.1, double.MaxValue, ErrorMessage = "Weight must be greater than zero.")]
        public double Weight { get; set; } // Example: 2.5 (kg)

        // ✅ Store as List<string> for sizes
        [Required(ErrorMessage = "At least one size is required.")]
        public List<string> SizeNames { get; set; } // Example: ["Small", "Medium", "Large"]

        // ✅ Store as List<string> for colors
        [Required(ErrorMessage = "At least one color is required.")]
        public List<string> ColorNames { get; set; } // Example: ["Red", "Blue", "Green"]
    }
}
