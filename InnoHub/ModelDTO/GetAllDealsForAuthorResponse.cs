using InnoHub.Core.Models;

namespace InnoHub.ModelDTO
{
    public class GetAllDealsForAuthorResponse
    {
        public int DealId { get; set; }
        public string BusinessOwnerId { get; set; }

        public string BusinessOwnerName { get; set; }
        public string BusinessOwnerPictureUrl { get; set; }
        public string BusinessName { get; set; }

        public string Description { get; set; }

        public decimal OfferMoney { get; set; } // Use decimal for monetary values

        public decimal OfferDeal { get; set; } // Use decimal for precision
        public decimal ManufacturingCost { get; set; }
        public decimal EstimatedPrice { get; set; }

        public List<string> Pictures { get; set; } = new List<string>();

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsApproved { get; set; }
        public string ApprovedAt { get; set; }
    }
}
