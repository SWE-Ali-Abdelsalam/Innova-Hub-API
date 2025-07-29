using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class DealProfit
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public Deal Deal { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ManufacturingCost { get; set; }
        public decimal OtherCosts { get; set; }
        public decimal NetProfit { get; set; }
        public decimal InvestorShare { get; set; }
        public decimal OwnerShare { get; set; }
        public decimal PlatformFee { get; set; }
        public DateTime DistributionDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPaid { get; set; }
        public bool IsApprovedByAdmin { get; set; } = false;
        public string? AdminId { get; set; } // ID of the admin who approved it
        public DateTime? ApprovalDate { get; set; }
        public bool IsPending { get; set; } = true; // Initially pending
    }
}
