using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class DiscussOfferDTO
    {
        public int DealId { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(8000, MinimumLength = 2, ErrorMessage = "Message must be between 2 and 8000 characters.")]
        public string Message { get; set; }
    }
}
