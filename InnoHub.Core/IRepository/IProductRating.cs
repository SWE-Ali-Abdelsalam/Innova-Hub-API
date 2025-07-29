using System.Threading.Tasks;
using InnoHub.Core.Models;

namespace InnoHub.Core.IRepository
{
    public interface IProductRating : IGenericRepository<ProductRating>
    {
        Task<ProductRating> GetRatingByProductIdAndUserIdAsync(int productId, string userId);
    }
}
