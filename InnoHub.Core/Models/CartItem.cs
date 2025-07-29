using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnoHub.Core.Models
{
    public class CartItem 
    {
        public int Id { get; set; }
        [ForeignKey("Product")]
        public int ProductId { get; set; } // Nullable Foreign Key (fix for SET NULL)
        public Product Product { get; set; } // Navigation Property
        public int Quantity { get; set; } // Quantity of the product

        [ForeignKey("Cart")]
        public int CartId { get; set; } // Nullable for soft deletes
        public Cart Cart { get; set; } // Navigation property to Cart

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; } // Price of the product in the cart

        public CartItem(int productId, int quantity, decimal price)
        {
            ProductId = productId;
            Quantity = quantity;
            Price = price;
        }

        public CartItem() { }
    }
}
