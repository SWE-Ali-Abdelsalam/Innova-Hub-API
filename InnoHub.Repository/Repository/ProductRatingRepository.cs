using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class ProductRatingRepository :GenericRepository<ProductRating>, IProductRating
    {
        public ProductRatingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ProductRating> GetRatingByProductIdAndUserIdAsync(int productId, string userId)
        {
            return await _context.ProductRatings
                                 .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
        }

    }
}
