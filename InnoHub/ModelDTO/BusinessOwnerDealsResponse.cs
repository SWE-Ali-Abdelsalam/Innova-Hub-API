namespace InnoHub.ModelDTO
{
    public class BusinessOwnerDealsResponse
    {
        public string BusinessOwnerId { get; set; }
        public string BusinessOwnerName { get; set; }
        public List<DealResponse> Deals { get; set; } = new List<DealResponse>();
    }

}
