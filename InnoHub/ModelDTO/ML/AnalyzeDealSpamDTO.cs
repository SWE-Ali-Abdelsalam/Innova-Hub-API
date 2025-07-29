using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO.ML
{
    public class AnalyzeDealSpamDTO
    {
        [Required]
        public int DealId { get; set; }
    }
}
