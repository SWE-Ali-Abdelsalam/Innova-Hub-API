using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class VerifyIdCardDTO
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        public string RejectionReason { get; set; } = "";
    }
}
