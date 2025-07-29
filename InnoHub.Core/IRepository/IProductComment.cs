using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IProductComment :IGenericRepository<ProductComment>
    {
        Task<List<ProductComment>> GetCommentsByProductIdAsync(int productId);
    }
}
