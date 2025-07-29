using System.Text.Json.Serialization;
using InnoHub.Core.Models;

namespace InnoHub.ModelDTO
{
    public class ReportedProductViewModel
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
       
        public OwnerViewModel Owner { get; set; }

        public int ProductId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ProductName { get; set; }

    }
}
