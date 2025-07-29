using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface ICart : IGenericRepository<Cart>
    {
        
        public Task<bool> CheckIfProductExistsInCart(string userId, int productId);
        
            public  Task<Cart> GetCartBYUserId(string userId);
        public Task<Cart> CreateCart(string userId, int productId, int quantity);
        public Task<Cart> DeleteProductFromCart(string userId, int productId);

        public Task<Cart> ClearCart(string userId);

        public Task<string> GetAuthorCartBYAuthorId(string userId);

        public Task<IEnumerable<CartItem>> GetCartItems(string userId);

        public Task<Cart> UpdateProductQuantity(string userId, int productId, int quantity);
    }
}
