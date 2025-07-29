using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Models
{
    public class ProductComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Who made the comment

        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }

        [Required]
        public int ProductId { get; set; } // Which product the comment belongs to

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }

        public string CommentText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
