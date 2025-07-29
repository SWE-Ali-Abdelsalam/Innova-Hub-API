using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IDealChangeRequest : IGenericRepository<DealChangeRequest>
    {
        Task<DealChangeRequest?> GetWithDetailsAsync(int requestId);
        Task<IEnumerable<DealChangeRequest>> GetPendingRequestsForUserAsync(string userId);
        Task<DealChangeRequest?> GetPendingRequestForDealAsync(int dealId);
    }
}
