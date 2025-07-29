using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IOrderItem : IGenericRepository<OrderItem>
    {
        Task<IEnumerable<OrderItem>> GetByProductIdAndDateRange(int productId, DateTime startDate, DateTime endDate);
        Task<int> GetTotalQuantitySold(int productId, DateTime startDate, DateTime endDate);
    }
}
