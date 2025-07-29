namespace InnoHub.ModelDTO
{
    public class ApproveProfitDTO
    {
        public int ProfitDistributionId { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionReason { get; set; } = "";
    }
}
