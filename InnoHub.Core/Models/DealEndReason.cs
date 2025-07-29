using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public enum DealEndReason
    {
        Completed,          // Deal reached its scheduled end date
        OwnerTerminated,    // Business owner terminated the deal
        InvestorTerminated, // Investor terminated the deal
        AdminTerminated,    // Admin terminated the deal
        Bankruptcy,         // Business bankruptcy
        MutualAgreement,    // Both parties agreed to terminate
        Breach,             // Due to breach of contract
        Other               // Other reasons
    }
}
