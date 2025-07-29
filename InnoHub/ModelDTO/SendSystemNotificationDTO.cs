using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class SendSystemNotificationDTO
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    }
}
