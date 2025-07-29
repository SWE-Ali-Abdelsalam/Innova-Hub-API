using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class CategoryNameWithAllProducts
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public List<AllProductsOnspecificCategoryDTO> AllProductsOnspecificCategories { get; set; }
       
    }
}
