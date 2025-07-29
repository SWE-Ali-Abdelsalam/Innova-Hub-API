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
    public class PaymentFailureLogRepository : GenericRepository<PaymentFailureLog>, IPaymentFailureLog
    {
        public PaymentFailureLogRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
