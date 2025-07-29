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
    public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItem
    {
        public OrderItemRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrderItem>> GetAllAsync()
        {
            return await _context.OrderItems
                .Include(p => p.Product)
                .Include(p => p.Order)
                .ToListAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetByProductIdAndDateRange(int productId, DateTime startDate, DateTime endDate)
        {
            return await _context.OrderItems
                .Include(item => item.Order)
                .Where(item =>
                    item.ProductId == productId &&
                    item.Order.OrderDate >= startDate &&
                    item.Order.OrderDate <= endDate)
                .ToListAsync();
        }

        public async Task<int> GetTotalQuantitySold(int productId, DateTime startDate, DateTime endDate)
        {
            return await _context.OrderItems
                .Include(item => item.Order)
                .Where(item =>
                    item.ProductId == productId &&
                    item.Order.OrderDate >= startDate &&
                    item.Order.OrderDate <= endDate)
                .SumAsync(item => item.Quantity);
        }
    }
}
