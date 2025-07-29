using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IWishlistItem :IGenericRepository<WishlistItem>
    {
        public Task<List<WishlistItem>> GetWishlistItemsByWishlistId(int wishlistId);
    }
}
