namespace InnoHub.ModelDTO
{
    public class ProductResponseDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductAuthorId { get; set; }
        public string ProductAuthorName { get; set; }
        public string ProductHomePicture { get; set; }
        public List<string> ProductPictures { get; set; }
        public string ProductDescription { get; set; }
        public double ProductWeight { get; set; }
        public string ProductDimensions { get; set; }
        public List<string> ProductSizes { get; set; }
        public List<string> ProductColors { get; set; }
        public decimal ProductPriceBeforeDiscount { get; set; }
        public decimal ProductPriceAfterDiscount { get; set; }
        public int ProductStock { get; set; }
        public double ProductRate { get; set; }
        public int NumberOfRatings { get; set; }
    }
}
