using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class Report
    {

        public int Id { get; set; }
        public string ReporterId { get; set; }
        public AppUser Reporter { get; set; }
        public string ReportedId { get; set; }
        public ReportedEntityType ReportedType { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
