namespace InnoHub.ModelDTO
{
    public class RespondToDealDeletionDTO
    {
        public int DeletionRequestId { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionReason { get; set; } = "";
    }
}
