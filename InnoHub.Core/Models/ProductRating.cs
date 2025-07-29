using InnoHub.Core.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class ProductRating 
{
    public string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; }

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; }

    [Required, Range(1, 5)]
    public int RatingValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
