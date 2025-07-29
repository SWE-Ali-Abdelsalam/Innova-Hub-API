using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public enum DealStatus
    {
        Pending,        // Investor made offer but owner hasn't accepted
        OwnerAccepted,  // Owner accepted, awaiting admin approval
        AdminApproved,  // Admin approved, contract sent
        Active,         // Investment active, contract signed
        Completed,      // Investment completed (reached end date)
        Terminated,     // Investment terminated before completion
        Rejected,       // Rejected by owner or admin
        Expired,        // Expired without action
        Renewed         // Renewed for another term
    }
}
