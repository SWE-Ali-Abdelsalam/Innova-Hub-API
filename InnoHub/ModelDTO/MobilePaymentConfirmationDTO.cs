using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class MobilePaymentConfirmationDTO
    {
        [Required]
        public string PaymentIntentId { get; set; }
    }
}
