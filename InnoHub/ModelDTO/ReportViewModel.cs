namespace InnoHub.ModelDTO
{
    public class ReportViewModel
    {
        public int ReportId { get; set; }
        public string ReporterId { get; set; }
        public string ReporterName { get; set; }
      //  public int ReportedId { get; set; }
        public string ReportedType { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Index { get; set; }  // New property to track the index
        public ReportedUserViewModel ReportedUser { get; set; }
        public ReportedDealViewModel ReportedDeal { get; set; }
        public ReportedProductViewModel ReportedProduct { get; set; }
    }

}
