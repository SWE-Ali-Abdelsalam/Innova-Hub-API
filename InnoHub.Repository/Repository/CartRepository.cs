using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class CartRepository : GenericRepository<Cart>, ICart
    {
        public CartRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Cart> GetCartBYUserId(string userId)
        {
            return await _context.Carts
                .Include(c=>c.User)
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product) // Ensure Product is loaded
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }


        public async Task<Cart> CreateCart(string userId, int productId, int quantity)
        {
            var cart = await GetCartBYUserId(userId) ?? new Cart
            {
                UserId = userId,
                CartItems = new List<CartItem>(),
                TotalPrice = 0
            };

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || quantity > product.Stock) return null;

            var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem != null)
            {
                int newQuantity = cartItem.Quantity + quantity;
                if (newQuantity > product.Stock)
                    return null;

                cartItem.Quantity = newQuantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    Price = product.Price * (1 - product.Discount / 100),
                });
            }

            cart.TotalPrice = cart.CartItems.Sum(i => i.Price * i.Quantity);

            if (cart.Id == 0)
                await _context.Carts.AddAsync(cart);

            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<bool> CheckIfProductExistsInCart(string userId, int productId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems == null)
                return false;

            return cart.CartItems.Any(i => i.ProductId == productId);
        }

        public async Task<Cart> DeleteProductFromCart(string userId, int productId)
        {
            var cart = await GetCartBYUserId(userId);
            if (cart == null) return null;

            var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cart.CartItems.Remove(cartItem);
                cart.TotalPrice = cart.CartItems.Sum(i => i.Price * i.Quantity);

                // Explicitly mark the cart as updated
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<Cart> ClearCart(string userId)
        {
            var cart = await GetCartBYUserId(userId);
            if (cart == null) return null;

            cart.CartItems.Clear();
            cart.TotalPrice = 0;

            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<IEnumerable<CartItem>> GetCartItems(string userId)
        {
            var cart = await GetCartBYUserId(userId);
            return cart?.CartItems ?? Enumerable.Empty<CartItem>();
        }

        public async Task<Cart> UpdateProductQuantity(string userId, int productId, int quantity)
        {
            var cart = await GetCartBYUserId(userId);
            if (cart == null) return null;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return null;

            var cartItem = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem != null)
            {
                int newQuantity = cartItem.Quantity + quantity;

                // Ensure new quantity does not exceed stock
                if (newQuantity > product.Stock)
                    return null;

                // Remove item if quantity drops to zero or below
                if (newQuantity <= 0)
                {
                    cart.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = newQuantity;
                }

                // Update total price
                cart.TotalPrice = cart.CartItems.Sum(i => i.Price * i.Quantity);

                // Ensure the total price is not negative
                if (!cart.CartItems.Any())
                    cart.TotalPrice = 0;

                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();

                return cart;
            }

            // If product is not in the cart, do nothing and return null
            return null;
        }
        
        public async Task<string> GetAuthorCartBYAuthorId(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            return user.FirstName + " " + user.LastName;
        }
    }
}
