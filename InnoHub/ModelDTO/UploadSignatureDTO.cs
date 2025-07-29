using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UploadSignatureDTO
    {
        [Required]
        public IFormFile SignatureImage { get; set; }
    }
}
