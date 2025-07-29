using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class SignContractDTO
    {
        [Required]
        public int InvestmentId { get; set; }
    }
}
