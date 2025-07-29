using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IDealDeleteRequest : IGenericRepository<DealDeleteRequest>
    {
        Task<DealDeleteRequest?> GetWithDetailsAsync(int requestId);
        Task<IEnumerable<DealDeleteRequest>> GetPendingRequestsForUserAsync(string userId);
        Task<DealDeleteRequest?> GetPendingRequestForDealAsync(int dealId);
    }
}
