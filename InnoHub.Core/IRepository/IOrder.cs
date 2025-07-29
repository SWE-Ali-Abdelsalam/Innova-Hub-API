using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IOrder : IGenericRepository<Order>
    {
        Task<IEnumerable<Order>> GetAllOrdersForSpecificUser(string UserId);
        Task<Order> GetByPaymentIntentId(string paymentIntentId);
        public (decimal subtotal, decimal tax, decimal totalAmount) CalculateTotals(Cart cart, decimal shippingCost);
    }
}
