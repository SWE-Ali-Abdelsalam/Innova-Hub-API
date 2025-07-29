using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class OrderReturnRequestRepository : GenericRepository<OrderReturnRequest>, IOrderReturnRequest
    {
        public OrderReturnRequestRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
