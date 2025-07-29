using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IDealTransaction : IGenericRepository<DealTransaction>
    {
        Task<IEnumerable<DealTransaction>> GetTransactionsByDealId(int dealId);
        Task<decimal> GetTotalAmountByType(int dealId, TransactionType type);
    }
}
