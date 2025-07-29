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
    public class ProductCommentRepository : GenericRepository<ProductComment>, IProductComment
    {
        public ProductCommentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<List<ProductComment>> GetCommentsByProductIdAsync(int productId)
        {
            return _context.ProductComments.Where(p=>p.ProductId == productId).Include(c=>c.User).ToListAsync();
        }
    }
}
