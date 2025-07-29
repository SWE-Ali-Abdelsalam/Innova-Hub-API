using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class RecordProfitDTO
    {
        public int DealId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
