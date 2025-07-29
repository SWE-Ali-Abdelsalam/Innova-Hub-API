using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class SendDealNotificationDTO
    {
        [Required]
        public int DealId { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
    }
}
