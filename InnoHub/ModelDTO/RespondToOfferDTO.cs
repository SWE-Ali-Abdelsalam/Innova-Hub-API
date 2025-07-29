using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class RespondToOfferDTO
    {
        public int DealId { get; set; }

        [Required(ErrorMessage = "Investor Id is required.")]
        public string InvestorId { get; set; }
        public bool IsAccepted { get; set; }
        public string Message { get; set; } = "";
    }
}
