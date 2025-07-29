using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class ProductPicture
    {
        public int Id { get; set; }

        [Required]
        public string PictureUrl { get; set; }

        // Foreign Key to Product
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
