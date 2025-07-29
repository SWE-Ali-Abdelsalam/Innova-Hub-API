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
    public class DealChangeRequestRepository : GenericRepository<DealChangeRequest>, IDealChangeRequest
    {
        public DealChangeRequestRepository(ApplicationDbContext context) : base(context) { }

        public async Task<DealChangeRequest?> GetWithDetailsAsync(int requestId)
        {
            return await _context.DealChangeRequests
                .Include(r => r.Deal)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task<IEnumerable<DealChangeRequest>> GetPendingRequestsForUserAsync(string userId)
        {
            // Get all deals owned by this user that have pending change requests
            var ownedDeals = await _context.Deals
                .Where(d => d.AuthorId == userId)
                .Select(d => d.Id)
                .ToListAsync();

            // Get pending change requests for those deals
            return await _context.DealChangeRequests
                .Include(r => r.Deal)
                .Where(r => ownedDeals.Contains(r.DealId.Value) && r.Status == ChangeRequestStatus.Pending)
                .ToListAsync();
        }

        public async Task<DealChangeRequest?> GetPendingRequestForDealAsync(int dealId)
        {
            return await _context.DealChangeRequests
                .Where(r => r.DealId == dealId && r.Status == ChangeRequestStatus.Pending)
                .FirstOrDefaultAsync();
        }
    }
}
