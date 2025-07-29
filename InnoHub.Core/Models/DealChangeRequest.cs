using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class DealChangeRequest
    {
        public int Id { get; set; }
        public int? DealId { get; set; }
        public Deal? Deal { get; set; }
        public string RequestedById { get; set; } // User ID of requester (business owner)
        public string OriginalValues { get; set; } // JSON of original values
        public string RequestedValues { get; set; } // JSON of requested changes
        public string? Notes { get; set; }
        public DateTime RequestDate { get; set; }
        public ChangeRequestStatus Status { get; set; }
        public string? ApprovedById { get; set; } // User ID of approver (investor)
        public DateTime? ApprovalDate { get; set; }
        public string? RejectionReason { get; set; }
    }
}
