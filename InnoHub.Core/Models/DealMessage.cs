using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class DealMessage
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public Deal Deal { get; set; }
        public string SenderId { get; set; }
        public AppUser Sender { get; set; }
        public string RecipientId { get; set; }
        public AppUser Recipient { get; set; }
        public string MessageText { get; set; }
        public int? ChangeRequestId { get; set; }
        public int? DeletionRequestId { get; set; }
        public int? ProfitDistributionId { get; set; }
        public string? ContractUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public MessageType MessageType { get; set; }
    }
}
