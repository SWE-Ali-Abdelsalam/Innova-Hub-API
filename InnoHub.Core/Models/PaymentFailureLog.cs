using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class PaymentFailureLog
    {
        public int Id { get; set; } // Primary Key
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string PaymentIntentId { get; set; }
        public string FailureReason { get; set; }
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    }
}
