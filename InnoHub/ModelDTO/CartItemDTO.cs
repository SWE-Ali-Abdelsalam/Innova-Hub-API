namespace InnoHub.ModelDTO
{
    public class CartItemDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string HomePictureUrl { get; set; }
        public List<string> ProductPictures { get; set; }
        public string ProductOwnerName { get; set; }
        public string ProductOwnerId { get; set; }
    }

}
