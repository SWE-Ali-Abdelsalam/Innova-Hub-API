using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UpdateShippingDTO
    {
        [Required(ErrorMessage = "DeliveryMethodId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "DeliveryMethodId must be greater than zero.")]
        public int DeliveryMethodId { get; set; }
    }
}
