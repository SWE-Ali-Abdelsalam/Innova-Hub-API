using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Wishlist 
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } // Link to the AppUser
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; }

    // ✅ Removed `ProductId`, as products are stored in `WishlistItem`

    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>(); // ✅ Correct way to store products in wishlist
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
