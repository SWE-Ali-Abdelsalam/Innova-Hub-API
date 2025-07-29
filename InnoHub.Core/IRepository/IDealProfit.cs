using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IDealProfit : IGenericRepository<DealProfit>
    {
        Task<IEnumerable<DealProfit>> GetProfitsByDealId(int dealId);
        Task<decimal> GetTotalProfitForInvestor(string investorId);
        Task<decimal> GetTotalProfitForOwner(string ownerId);
        Task<DealProfit?> GetMostRecentProfitDistribution(int dealId);
        Task<decimal> GetTotalProfitForDeal(int dealId);
        Task<decimal> GetTotalProfitForInvestor(string investorId, int dealId);
        Task<decimal> GetTotalProfitForOwner(string ownerId, int dealId);
        Task<DealProfit?> GetProfitDistributionForPeriod(int dealId, DateTime startDate, DateTime endDate);
    }
}
