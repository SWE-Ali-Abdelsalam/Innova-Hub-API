using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class PaymentRefundLog
    {
         public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundId { get; set; }
        public string RefundStatus { get; set; }
        public DateTime RefundCreated { get; set; }
    }
}
