using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class SendNotificationDTO
    {
        [Required]
        [StringLength(450)]
        public string TargetUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        public string NotificationType { get; set; } = "General";
    }
}
