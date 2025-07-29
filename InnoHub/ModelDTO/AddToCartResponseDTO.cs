using InnoHub.Core.Models;

namespace InnoHub.ModelDTO
{
    public class AddToCartResponseDTO
    {
        public int NumberOfProducts { get; set; }
        public string CartAuthorId {  get; set; }
        public string CartAuthorName { get; set; }
        public ICollection<CartItemDTO> cartItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
