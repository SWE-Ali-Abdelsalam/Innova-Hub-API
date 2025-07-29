using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class OrderReturnRequest
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string UserId { get; set; }
        public string ReturnReason { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
