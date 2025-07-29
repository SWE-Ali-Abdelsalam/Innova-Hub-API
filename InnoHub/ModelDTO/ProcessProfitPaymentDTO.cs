using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class ProcessProfitPaymentDTO
    {
        [Required]
        public int ProfitDistributionId { get; set; }

        /// <summary>
        /// Optional period in format "MMM yyyy" (e.g., "Jan 2025").
        /// If not provided, the previous month will be used.
        /// </summary>
        //public string? Period { get; set; }
    }
}
