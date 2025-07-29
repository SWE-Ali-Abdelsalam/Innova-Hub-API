using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public enum MessageType
    {
        General,
        OfferAccepted,
        AdminApprovalRequired,
        CompletionRequested,
        TerminationRequested,
        ProfitRecorded,
        EditRequested,
        OfferDiscussion,
        RenewalRequested
    }
}
