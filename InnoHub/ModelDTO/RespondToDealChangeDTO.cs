namespace InnoHub.ModelDTO
{
    public class RespondToDealChangeDTO
    {
        public int ChangeRequestId { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionReason { get; set; } = "";
    }
}
