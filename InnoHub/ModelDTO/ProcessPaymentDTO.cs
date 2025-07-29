using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class ProcessPaymentDTO
    {
        public int DealId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be more than or equal 1 month")]
        public int DurationInMonths { get; set; } = 12;
    }
}
