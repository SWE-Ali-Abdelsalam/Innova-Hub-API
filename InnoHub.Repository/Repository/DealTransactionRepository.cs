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
    public class DealTransactionRepository : GenericRepository<DealTransaction>, IDealTransaction
    {
        public DealTransactionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<DealTransaction>> GetTransactionsByDealId(int dealId)
        {
            return await _context.DealTransactions
                .Where(t => t.DealId == dealId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountByType(int dealId, TransactionType type)
        {
            return await _context.DealTransactions
                .Where(t => t.DealId == dealId && t.Type == type)
                .SumAsync(t => t.Amount);
        }
    }
}
