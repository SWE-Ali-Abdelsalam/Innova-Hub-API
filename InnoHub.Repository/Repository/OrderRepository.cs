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
    public class OrderRepository : GenericRepository<Order>, IOrder
    {
        public OrderRepository(ApplicationDbContext context) : base(context)
        {

        }
        public async Task<Order> GetByPaymentIntentId(string paymentIntentId)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);
        }
        public (decimal subtotal, decimal tax, decimal totalAmount) CalculateTotals(Cart cart, decimal shippingCost)
        {
            decimal subtotal = cart.CartItems.Sum(item => item.Quantity * item.Product.Price);
            decimal tax = subtotal * 0.02m;
            decimal totalAmount = subtotal + tax + shippingCost;
            return (subtotal, tax, totalAmount);
        }

        public async Task<IEnumerable<Order>> GetAllOrdersForSpecificUser(string UserId)
        {
            if (string.IsNullOrEmpty(UserId))
            {
                return null;
            }

            return await _context.Orders
                                 .Where(o => o.UserId == UserId)
                                 .Include(o => o.User)
                                 .Include(o=>o.OrderItems)
                                 .ThenInclude(o=>o.Product)
                                 .ToListAsync();  // Ensure that the query is executed asynchronously
        }
    }
}
