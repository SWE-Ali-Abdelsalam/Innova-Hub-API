using System.Collections.Generic;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IWishlist : IGenericRepository<Wishlist>
    {
        
        Task<Wishlist> GetWishlistByUserID(string userId);
        Task<bool> RemoveProductFromWishlist(int productId, string userId);
    }
}
