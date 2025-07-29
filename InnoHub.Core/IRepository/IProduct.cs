using InnoHub.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IProduct : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetAllWithCategoriesAsync();
        public Task<List<Product>> GetBestSellingProductsAsync(int top);
        public IQueryable<Product> GetQueryable();
        public Task RemoveAllPicturesByProductIdAsync(int productId);
        Task<IList<Product>> GetAllProductsByCategoryId(int categoryId);
        Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> productIds);
        Task<bool> UpdateProductAsync(Product product);
        Task<IEnumerable<Product>> GetProductLinkedDeals(string ownerId);
    }
}
