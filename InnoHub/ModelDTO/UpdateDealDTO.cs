using InnoHub.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UpdateDealDTO
    {
        //[Required(ErrorMessage = "First name is required.")]
        [StringLength(8000, ErrorMessage = "Business Name cannot exceed 100 characters.")]
        public string? BusinessName { get; set; }

        [StringLength(8000, ErrorMessage = "Description cannot exceed 8000 characters.")]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Offer Money must be greater than zero.")]
        public decimal? OfferMoney { get; set; }

        [Range(0, 100, ErrorMessage = "Offer Deal must be between 0% and 100%.")]
        public decimal? OfferDeal { get; set; }

        //[MinLength(1, ErrorMessage = "You must upload at least one picture.")]
        public List<IFormFile>? Pictures { get; set; } = new List<IFormFile>();

        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be more than or equal 1")]
        public int? CategoryId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? ManufacturingCost { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? EstimatedPrice { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be more than or equal 1")]
        public int? DurationInMonths { get; set; }
    }
}
