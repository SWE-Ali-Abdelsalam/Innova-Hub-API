using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class SendRoleNotificationDTO
    {
        [Required]
        [StringLength(50)]
        public string Role { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
    }
}
