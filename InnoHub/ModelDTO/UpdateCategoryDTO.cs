using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UpdateCategoryDTO
    {
        [Required(ErrorMessage = "Category ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number.")]
        public int CategoryID { get; set; }

        [StringLength(100, MinimumLength = 3, ErrorMessage = "Category name must be between 3 and 100 characters.")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? ImageUrl { get; set; }

        public bool? IsPopular { get; set; }
    }
}
