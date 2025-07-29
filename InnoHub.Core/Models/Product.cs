using InnoHub.Core.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
namespace InnoHub.Core.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required, Url]
        public string HomePicture { get; set; }

        // Navigation property for related ProductPictures
        public ICollection<ProductPicture> ProductPictures { get; set; } = new List<ProductPicture>();

        [Required, Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Range(0, 100)]
        public decimal Discount { get; set; }

        [Required]
        public string AuthorId { get; set; }
        public AppUser Author { get; set; }

        [Required]
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<ProductRating> Ratings { get; set; } = new List<ProductRating>();

        [NotMapped]
        public double AverageRating => Ratings.Any() ? Math.Round(Ratings.Average(r => r.RatingValue), 2) : 0;

        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public ICollection<CartItem> CartItems { get; set; }

        public string? Dimensions { get; set; }
        public double? Weight { get; set; }

        [NotMapped]
        public int TotalSold { get; set; } // This field will hold sales data
        public ICollection<ProductComment> Comments { get; set; }

        // Navigation properties for Sizes and Colors
        public ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();
        public ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();
        public ICollection<Deal> Deals { get; set; } = new List<Deal>();

        //================================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? SubCategory { get; set; }
    }

}


