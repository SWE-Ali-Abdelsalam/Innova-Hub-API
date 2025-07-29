using InnoHub.Core.Models;

namespace InnoHub.ModelDTO
{
    public class ProposeChangesDTO
    {
        public int DealId { get; set; }
        public ChangeRequestType RequestType { get; set; }
        public Dictionary<string, object> ChangedValues { get; set; }
        public string? RequestNotes { get; set; }
    }
}
