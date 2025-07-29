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
    public class DealDeleteRequestRepository : GenericRepository<DealDeleteRequest>, IDealDeleteRequest
    {
        public DealDeleteRequestRepository(ApplicationDbContext context) : base(context) { }

        public async Task<DealDeleteRequest?> GetWithDetailsAsync(int requestId)
        {
            return await _context.DealDeleteRequests
                .Include(r => r.Deal)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task<IEnumerable<DealDeleteRequest>> GetPendingRequestsForUserAsync(string userId)
        {
            // Get all deals owned by this user that have pending delete requests
            var ownedDeals = await _context.Deals
                .Where(d => d.AuthorId == userId)
                .Select(d => d.Id)
                .ToListAsync();

            // Get pending delete requests for those deals
            return await _context.DealDeleteRequests
                .Include(r => r.Deal)
                .Where(r => ownedDeals.Contains(r.DealId) && r.Status == DeleteRequestStatus.Pending)
                .ToListAsync();
        }

        public async Task<DealDeleteRequest?> GetPendingRequestForDealAsync(int dealId)
        {
            return await _context.DealDeleteRequests
                .Where(r => r.DealId == dealId && r.Status == DeleteRequestStatus.Pending)
                .FirstOrDefaultAsync();
        }
    }
}
