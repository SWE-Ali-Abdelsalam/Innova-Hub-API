using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class MarkNotificationsReadDTO
    {
        [Required]
        public List<int> NotificationIds { get; set; } = new List<int>();
    }
}
