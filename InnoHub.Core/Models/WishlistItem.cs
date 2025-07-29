using System.ComponentModel.DataAnnotations.Schema;

namespace InnoHub.Core.Models
{
    public class WishlistItem 
    {
        public int Id { get; set; }
        // ✅ Link to Wishlist
        public int WishlistId { get; set; }
        [ForeignKey(nameof(WishlistId))]
        public Wishlist Wishlist { get; set; }

        // ✅ Link to Product
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }
    }
}
