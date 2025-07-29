using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class ConfirmMobilePaymentDTO
    {
        [Required(ErrorMessage = "PaymentIntentId is required.")]
        public string PaymentIntentId { get; set; }
    }
}
