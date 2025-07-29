using InnoHub.Core.Models;

namespace InnoHub.ModelDTO
{
    public class CreateReportDto
    {
        public string Type { get; set; }    // Type of entity being reported (e.g., User, Deal, Product)
        public string TargetId { get; set; } // TargetId can now be string to handle both GUID and integer
        public string Description { get; set; }
    }
}
