namespace InnoHub.ModelDTO
{
    public class InvestmentMessageDTO
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string RecipientName { get; set; }
        public string MessageText { get; set; }
        public int? ChangeRequestId { get; set; }
        public int? DeletionRequestId { get; set; }
        public int? ProfitDistributionId { get; set; }
        public string? ContractUrl { get; set; }
        public bool IsRead { get; set; }
        public string MessageType { get; set; }
        public string CreatedAt { get; set; }
    }
}
