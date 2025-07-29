using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class ProductRecommendation
    {
        public string ProductName { get; set; }
        public double Score { get; set; } // For personalized recommendations only
    }
}
