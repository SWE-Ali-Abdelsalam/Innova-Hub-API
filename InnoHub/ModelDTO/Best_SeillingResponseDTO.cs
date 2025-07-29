namespace InnoHub.ModelDTO
{
    public class Best_SeillingResponseDTO
    {
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }  // Fixed
        public string HomePictureUrl { get; set; }  // Fixed naming
        public List<string> ProductPictures { get; set; }  // Changed type from string to List<string>
        public double PriceBeforeDiscount { get; set; }
        public double PriceAfterDiscount { get; set; }
        public int Stock { get; set; }
        public double Rating { get; set; }
        public int NumberOfRatings { get; set; }
        public Dictionary<string, double>? RatingBreakdown { get; set; }
    }
}
