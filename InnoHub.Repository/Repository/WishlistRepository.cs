using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class WishlistRepository : GenericRepository<Wishlist>, IWishlist
    {
        

        public WishlistRepository(ApplicationDbContext context) : base(context)
        {
            
        }

        /// <summary>
        /// Get a specific wishlist item by Product ID and User ID.
        /// </summary>
        /// <param name="productId">ID of the product.</param>
        /// <param name="userId">ID of the user.</param>
        /// <returns>The wishlist item if it exists; otherwise, null.</returns>

        /// <summary>
        /// Get all wishlist items for a specific user.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <returns>A list of wishlist items for the user.</returns>
        public async Task<Wishlist> GetWishlistByUserID(string userId)
        {
            return await _context.Wishlists
                .Include(w => w.WishlistItems)   // Include WishlistItems
                    .ThenInclude(wi => wi.Product)  // Include Product for each WishlistItem
                        .ThenInclude(p => p.ProductPictures)  // Include ProductPictures for each Product
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }


        public async Task<bool> RemoveProductFromWishlist(int productId, string userId)
        {
            // ✅ Retrieve wishlist including its items
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist == null)
            {
                Console.WriteLine("Wishlist not found for user: " + userId);
                return false;
            }

            if (wishlist.WishlistItems == null || !wishlist.WishlistItems.Any())
            {
                Console.WriteLine("Wishlist exists, but has no items.");
                return false;
            }

            // ✅ Find the product in the user's wishlist
            var wishlistItem = wishlist.WishlistItems.FirstOrDefault(wi => wi.ProductId == productId);
            if (wishlistItem == null)
            {
                Console.WriteLine($"ProductId {productId} not found in wishlist.");
                return false;
            }

            // ✅ Remove the item from the wishlist
            _context.WishlistItems.Remove(wishlistItem);
             _context.SaveChanges();

            Console.WriteLine($"ProductId {productId} removed successfully from wishlist.");
            return true;
        }


    }
}
