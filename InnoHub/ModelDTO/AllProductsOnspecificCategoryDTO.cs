namespace InnoHub.ModelDTO
{
    public class AllProductsOnspecificCategoryDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public double ProductWeight { get; set; }
        public string ProductDimensions { get; set; }

        public string HomePicture { get; set; }

        public List<string> ProductPictures { get; set; } = new List<string>();

        public decimal ProductPrice { get; set; }
        public bool IsAvailable { get; set; }
        public int Stock { get; set; } 
       public string AuthorName { get; set; }
    }
}
