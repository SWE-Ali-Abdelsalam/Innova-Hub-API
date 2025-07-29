namespace InnoHub.ModelDTO
{
    public class ProductViewModel
    {
       
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string HomePicture { get; set; }
        public List<string> Pictures { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public string Dimensions { get; set; }
        public string AuthorName { get; set; }
        public decimal PriceAfterDiscount { get; set; }
        public decimal PriceBeforeDiscount { get; set; }
        public int Stock {  get; set; }
        public double? AverageRating { get; set; } // Nullable Rate
        
    }
}
