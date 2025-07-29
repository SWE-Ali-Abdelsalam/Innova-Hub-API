using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IDeal :IGenericRepository<Deal>
    {
        Task<IEnumerable<Deal>> GetDealsByApprovalAsync(int page, int pageSize, bool isApproved);
        Task<ICollection<Deal>> GetAllDealsForSpecificAuthor(string authorId);
        Task<ICollection<Deal>> GetAllDealsAsync();
        Task<Deal?> GetDealByIdWithAuthorAndCategory(int dealId);
        Task<bool> HasActiveDealsAsync(int dealId);
        //Task<IEnumerable<Deal>> GetActiveDealsAsync(int dealId);
        Task<Deal?> GetDealWithDetails(int dealId);
        Task<IEnumerable<Deal>> GetDealsByInvestorId(string investorId);
        Task<IEnumerable<Deal>> GetDealsByOwnerId(string ownerId);
        Task<IEnumerable<Deal>> GetPendingDealsForAdmin();
        Task<Deal?> GetDealsByProductId(int productId);
        Task<IEnumerable<Deal>> GetDealsEligibleForProduct(string ownerId);
        Task<IEnumerable<Deal>> GetDealsApproachingEndDate(int daysThreshold);
        Task<IEnumerable<Deal>> GetDealsReachedEndDate();
        Task<IEnumerable<Deal>> GetDealsNeedAdminApproval();
        Task<Deal?> GetByPaymentIntentId(string paymentIntentId);
        Task<IEnumerable<Deal>> GetActiveDealsByInvestorId(string investorId);
    }
}
