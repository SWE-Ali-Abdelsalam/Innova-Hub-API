using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InnoHub.Repository.Repository
{
    public class DealProfitRepository : GenericRepository<DealProfit>, IDealProfit
    {
        public DealProfitRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<DealProfit>> GetProfitsByDealId(int dealId)
        {
            return await _context.DealProfits
                .Where(p => p.DealId == dealId)
                .OrderByDescending(p => p.DistributionDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalProfitForInvestor(string investorId)
        {
            return await _context.DealProfits
                .Include(p => p.Deal)
                .Where(p => p.Deal.InvestorId == investorId && p.IsPaid)
                .SumAsync(p => p.InvestorShare);
        }

        public async Task<decimal> GetTotalProfitForOwner(string ownerId)
        {
            return await _context.DealProfits
                .Include(p => p.Deal)
                .Where(p => p.Deal.AuthorId == ownerId && p.IsPaid)
                .SumAsync(p => p.OwnerShare);
        }
        public async Task<DealProfit?> GetMostRecentProfitDistribution(int dealId)
        {
            return await _context.DealProfits
                .Where(p => p.DealId == dealId)
                .OrderByDescending(p => p.DistributionDate)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetTotalProfitForDeal(int dealId)
        {
            return await _context.DealProfits
                .Where(p => p.DealId == dealId)
                .SumAsync(p => p.NetProfit);
        }

        public async Task<decimal> GetTotalProfitForInvestor(string investorId, int dealId)
        {
            return await _context.DealProfits
                .Where(p => p.DealId == dealId && p.IsPaid)
                .SumAsync(p => p.InvestorShare);
        }

        public async Task<decimal> GetTotalProfitForOwner(string ownerId, int dealId)
        {
            return await _context.DealProfits
                .Where(p => p.DealId == dealId && p.IsPaid)
                .SumAsync(p => p.OwnerShare);
        }

        public async Task<DealProfit?> GetProfitDistributionForPeriod(int dealId, DateTime startDate, DateTime endDate)
        {
            return await _context.DealProfits
                .FirstOrDefaultAsync(p => p.DealId == dealId && p.StartDate == startDate && p.EndDate == endDate);
        }
    }
}
