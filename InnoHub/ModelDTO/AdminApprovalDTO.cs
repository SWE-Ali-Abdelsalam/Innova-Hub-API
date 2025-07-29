using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class AdminApprovalDTO
    {
        public int DealId { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionReason { get; set; } = "";
    }
}
