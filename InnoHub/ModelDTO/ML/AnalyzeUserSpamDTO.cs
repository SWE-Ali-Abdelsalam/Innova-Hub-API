using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO.ML
{
    public class AnalyzeUserSpamDTO
    {
        [Required]
        public string UserId { get; set; } = "";
    }
}
