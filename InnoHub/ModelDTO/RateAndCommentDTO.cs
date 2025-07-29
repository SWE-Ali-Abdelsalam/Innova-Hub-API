using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class RateAndCommentDTO
    {
        public int ProductId { get; set; }


        [Required(ErrorMessage = "Rating value is required.")]

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int RatingValue { get; set; }
        public string Comment { get; set; } = "";
    }
}
