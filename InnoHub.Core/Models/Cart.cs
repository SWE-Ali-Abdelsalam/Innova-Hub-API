using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class Cart 
    {
        public int Id { get; set; }
        public AppUser User { get; set; }
        public string UserId { get; set; } // User's email address 
        public decimal TotalPrice { get; set; }
        public ICollection<CartItem> CartItems { get; set; }

        public Cart()
        {
            CartItems = new List<CartItem>();
        }
    }
}
