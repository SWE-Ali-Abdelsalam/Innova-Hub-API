using InnoHub.Core.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class DealTransaction
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public Deal Deal { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string TransactionId { get; set; } // Payment gateway transaction ID
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Description { get; set; }
    }
}
