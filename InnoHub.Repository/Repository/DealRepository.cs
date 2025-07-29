using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class DealRepository : GenericRepository<Deal>, IDeal
    {
        public DealRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Deal>> GetDealsByApprovalAsync(int page, int pageSize, bool isApproved)
        {
            var query = _context.Deals
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Where(d => d.IsApproved == isApproved)
                .OrderByDescending(d => d.Id); // Order by CreatedAt (descending)

        // Fetch deals with pagination
        return await query
            .Skip((page - 1) * pageSize) // Skip the previous pages
            .Take(pageSize) // Take the page size
            .ToListAsync();
        }

        public async Task<ICollection<Deal>> GetAllDealsAsync()
        {
            return await _context.Deals
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Where(d => d.IsApproved && d.IsVisible)
                .ToListAsync();
        }


        public async Task<ICollection<Deal>> GetAllDealsForSpecificAuthor(string authorId)
        {
            return await _context.Deals
                .Where(d => d.AuthorId == authorId).Include(d => d.Author).Include(d=>d.Category)
                .ToListAsync(); // ✅ Optimized for async database access
        }

        public async Task<Deal?> GetDealByIdWithAuthorAndCategory(int dealId)
        {
            return await _context.Deals
        .Include(d => d.Author)  // ✅ Include Author details
        .Include(d => d.Category) // ✅ Include Category details
        .FirstOrDefaultAsync(d => d.Id == dealId); // ✅ Fetch deal by dealId
        }

        public async Task<bool> HasActiveDealsAsync(int dealId)
        {
            return await _context.Deals
                .AnyAsync(i => i.Id == dealId &&
                              (i.Status == DealStatus.OwnerAccepted ||
                               i.Status == DealStatus.AdminApproved ||
                               i.Status == DealStatus.Active));
        }

        // ========= Investment =========

        public async Task<Deal?> GetDealWithDetails(int dealId)
        {
            return await _context.Deals
                .Include(d => d.Author)
                .Include(d => d.Investor)
                .Include(d => d.Category)
                .Include(d => d.Product)
                .Include(d => d.Messages)
                .Include(d => d.ProfitDistributions)
                .FirstOrDefaultAsync(d => d.Id == dealId);
        }


        public async Task<IEnumerable<Deal>> GetDealsByInvestorId(string investorId)
        {
            return await _context.Deals
                .Include(d => d.Author)
                .Include(i => i.ProfitDistributions)
                .Where(i => i.InvestorId == investorId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Deal>> GetDealsByOwnerId(string ownerId)
        {
            return await _context.Deals
                .Include(i => i.Investor)
                .Include(i => i.ProfitDistributions)
                .Where(i => i.AuthorId == ownerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Deal>> GetPendingDealsForAdmin()
        {
            return await _context.Deals
                .Include(d => d.Author)
                .Include(i => i.Investor)
                .Where(i => i.Status == DealStatus.OwnerAccepted)
                .ToListAsync();
        }

        public async Task<Deal?> GetDealsByProductId(int productId)
        {
            return await _context.Deals
                .Include(i => i.Investor)
                .Include(i => i.ProfitDistributions)
                .FirstOrDefaultAsync(i => i.ProductId == productId);
        }

        public async Task<IEnumerable<Deal>> GetDealsEligibleForProduct(string ownerId)
        {
            return await _context.Deals
                .Include(i => i.Investor)
                .Where(i => i.AuthorId == ownerId
                       && i.Status == DealStatus.Active
                       && i.IsReadyForProduct
                       && !i.IsProductCreated)
                .ToListAsync();
        }

        public async Task<IEnumerable<Deal>> GetDealsApproachingEndDate(int daysThreshold)
        {
            var today = DateTime.UtcNow.Date;
            var thresholdDate = today.AddDays(daysThreshold);

            return await _context.Deals
                .Include(i => i.Investor)
                .Where(i => i.Status == DealStatus.Active &&
                          i.ScheduledEndDate.HasValue &&
                          i.ScheduledEndDate.Value.Date <= thresholdDate &&
                          i.ScheduledEndDate.Value.Date > today)
                .ToListAsync();
        }

        public async Task<IEnumerable<Deal>> GetDealsReachedEndDate()
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Deals
                .Include(i => i.Investor)
                .Where(i => i.Status == DealStatus.Active &&
                          i.ScheduledEndDate.HasValue &&
                          i.ScheduledEndDate.Value.Date == today)
                .ToListAsync();
        }

        public async Task<IEnumerable<Deal>> GetDealsNeedAdminApproval()
        {
            return await _context.Deals
                .Include(d => d.Author)
                .Include(d => d.Investor)
                .Include(d => d.Category)
                .Where(i => i.Status == DealStatus.OwnerAccepted)
                .ToListAsync();
        }

        public async Task<Deal?> GetByPaymentIntentId(string paymentIntentId)
        {
            return await _context.Deals
                .Include(d => d.Author)
                .Include(d => d.Investor)
                .Include(d => d.Category)
                .Include(d => d.Product)
                .Include(d => d.Messages)
                .Include(d => d.ProfitDistributions)
                .FirstOrDefaultAsync(d => d.PaymentIntentId == paymentIntentId);
        }

        public async Task<IEnumerable<Deal>> GetActiveDealsByInvestorId(string investorId)
        {
            return await _context.Deals
                .Where(i => i.InvestorId == investorId &&
                          (i.Status == DealStatus.OwnerAccepted ||
                           i.Status == DealStatus.AdminApproved ||
                           i.Status == DealStatus.Active))
                .ToListAsync();
        }
    }
}
