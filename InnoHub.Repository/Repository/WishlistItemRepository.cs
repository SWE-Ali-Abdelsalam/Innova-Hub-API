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
    public class WishlistItemRepository : GenericRepository<WishlistItem>, IWishlistItem
    {
        public WishlistItemRepository(ApplicationDbContext context) : base(context)
        {

        }
        public async Task<List<WishlistItem>> GetWishlistItemsByWishlistId(int wishlistId)
        {
            return await _context.WishlistItems
                .Where(wi => wi.WishlistId == wishlistId)
                .Include(wi => wi.Product) // ✅ Ensure product details are included
                .ToListAsync();
        }
    }
}
