using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InnoHub.Core.Models
{
    public class Category 
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; }
        public ICollection<Deal> Deals { get; set; } = new List<Deal>();

        public bool IsPopular { get; set; }
    }
}
