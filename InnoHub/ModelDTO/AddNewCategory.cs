using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class AddNewCategory
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Category name must be between 3 and 100 characters.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Required(ErrorMessage = "Category Description is required.")]
        public string Description { get; set; }

        [DataType(DataType.Upload)]
        
        public IFormFile ImageUrl { get; set; }

        public bool IsPopular { get; set; }
    }
}
