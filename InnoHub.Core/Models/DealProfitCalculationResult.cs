using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class DealProfitCalculationResult
    {
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ManufacturingCost { get; set; }
        public decimal OtherCosts { get; set; }
        public decimal NetProfit { get; set; }
        public decimal InvestorShare { get; set; }
        public decimal OwnerShare { get; set; }
        public decimal PlatformFee { get; set; }
        public string Period { get; set; }

        // Additional metrics
        public IList<OrderItem> OrderItems { get; set; }
        public Dictionary<string, decimal> DailyRevenue { get; set; }
    }
}
