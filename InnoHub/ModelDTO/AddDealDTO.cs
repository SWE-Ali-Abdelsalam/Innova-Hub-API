using InnoHub.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class AddDealDTO//
    {
        [Required(ErrorMessage = "Business Name is required.")]
        [StringLength(8000, ErrorMessage = "Business Name cannot exceed 100 characters.")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(8000, ErrorMessage = "Description cannot exceed 8000 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Offer Money is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Offer Money must be greater than zero.")]
        public decimal OfferMoney { get; set; }

        [Required(ErrorMessage = "Offer Deal percentage is required.")]
        [Range(0, 100, ErrorMessage = "Offer Deal must be between 0% and 100%.")]
        public decimal OfferDeal { get; set; }

        [Required(ErrorMessage = "At least one picture is required.")]
        [MinLength(1, ErrorMessage = "You must upload at least one picture.")]
        public List<IFormFile> Pictures { get; set; } = new List<IFormFile>();

        [Required(ErrorMessage = "Category ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be more than or equal 1")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Manufacturing Cost is required.")]
        [Range(0.01, double.MaxValue)]
        public decimal ManufacturingCost { get; set; }

        [Required(ErrorMessage = "Estimated Price is required.")]
        [Range(0.01, double.MaxValue)]
        public decimal EstimatedPrice { get; set; }
    }
}
