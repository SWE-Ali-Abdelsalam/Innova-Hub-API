using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stripe;

namespace InnoHub.Core.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; } // Nullable Foreign Key (fix for SET NULL)
        public Product Product { get; set; } // Navigation Property
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice => Price * Quantity;
        [ForeignKey("Order")]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        //===================================================

        public decimal Profit { get; set; }
        public DateTime ShipDate { get; set; } = DateTime.UtcNow.AddMinutes(30);

        public OrderItem()
        {
            Profit = Price / 4;
        }
    }
}
