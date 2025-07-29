using InnoHub.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class TerminateDealDTO
    {
        public int DealId { get; set; }

        [EnumDataType(typeof(DealEndReason), ErrorMessage = "Invalid end reason.")]
        public DealEndReason EndReason { get; set; }
        public string TerminationNotes { get; set; } = "";
    }
}
