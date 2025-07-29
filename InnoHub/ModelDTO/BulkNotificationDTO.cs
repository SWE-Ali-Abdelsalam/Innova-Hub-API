using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class BulkNotificationDTO
    {
        [Required]
        public List<string> TargetUserIds { get; set; } = new List<string>();

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        public string NotificationType { get; set; } = "General";

        public DateTime? ScheduledAt { get; set; }
    }
}
