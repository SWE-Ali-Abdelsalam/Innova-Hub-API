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
    public class DeliveryMethodRepository : GenericRepository<DeliveryMethod>, IDeliveryMethod
    {
        public DeliveryMethodRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
