using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UploadIdCardDTO
    {
        [Required]
        public IFormFile FrontImage { get; set; }

        [Required]
        public IFormFile BackImage { get; set; }
    }
}
