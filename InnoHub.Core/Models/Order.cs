using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnoHub.Core.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // 🔹 User Information
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        // 🔹 Payment Information
        public string? PaymentIntentId { get; set; }  // Stripe Payment Intent ID
        public string? ClientSecret { get; set; }  // Stripe Client Secret

        // 🔹 Order Details
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

        // 🔹 One-to-One Relation with ShippingAddress
        public int ShippingAddressId { get; set; }

        [ForeignKey("ShippingAddressId")]
        public ShippingAddress ShippingAddress { get; set; }

        public int? DeliveryMethodId { get; set; }

        [ForeignKey("DeliveryMethodId")]
        public DeliveryMethod? DeliveryMethod { get; set; }
        public ReturnStatus ReturnStatus { get; set; } = ReturnStatus.None;

        // 🔹 Order Items
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public string UserComment { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; } = 0m;
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        //==========================================================

        public string? ShipMode { get; set; }
    }

}
