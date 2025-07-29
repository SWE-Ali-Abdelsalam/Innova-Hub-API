using InnoHub.UnitOfWork;
using InnoHub.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public WishlistController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("addProductToWishlist/{productId}")]
        public async Task<IActionResult> AddProductToWishlist(
      [FromHeader(Name = "Authorization")] string authorizationHeader, int productId)
        {
            // ✅ Extract user ID from token
            var userId =_unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid authorization token." });

            // ✅ Check if the product exists
            var product = await _unitOfWork.Product.GetByIdAsync(productId);
            if (product == null)
                return NotFound(new { Message = "Product not found." });

            // ✅ Retrieve user's wishlist
            var wishlist = await _unitOfWork.Wishlist.GetWishlistByUserID(userId);
            if (wishlist == null)
            {
                // ✅ Create a new wishlist for the user if it does not exist
                wishlist = new Wishlist
                {
                    UserId = userId,
                    WishlistItems = new List<WishlistItem>()
                };
                await _unitOfWork.Wishlist.AddAsync(wishlist);
                await _unitOfWork.Complete();
            }

            // ✅ Check if the product is already in the wishlist
            bool productExists = wishlist.WishlistItems.Any(wi => wi.ProductId == productId);
            if (productExists)
                return BadRequest(new { Message = "Product is already in the wishlist." });

            // ✅ Add the product to the wishlist as a WishlistItem
            var wishlistItem = new WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = productId
            };

            await _unitOfWork.WishlistItem.AddAsync(wishlistItem);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Product added to wishlist." });
        }

        // Get the user's wishlist
        [HttpGet("wishlist")]
        public async Task<IActionResult> GetWishlist([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // ✅ Extract user ID from the authorization token
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Invalid or missing authorization token." });
            }

            // ✅ Retrieve the wishlist for the user (including WishlistItems and Product details)
            var wishlist = await _unitOfWork.Wishlist.GetWishlistByUserID(userId);
            if (wishlist == null)
            {
                return Ok(new { Message = "Wishlist is empty.", Wishlist = new object[] { } });
            }

            // ✅ Ensure `WishlistItems` are loaded properly
            var wishlistItems = await _unitOfWork.WishlistItem.GetWishlistItemsByWishlistId(wishlist.Id);
            if (wishlistItems == null || !wishlistItems.Any())
            {
                return Ok(new { Message = "Wishlist is empty.", Wishlist = new object[] { } });
            }

            // ✅ Map wishlist items to response
            var response = wishlistItems.Select(item => new
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? "Unknown Product",
                ProductPrice = item.Product?.Price ?? 0,
                ProductDiscount = item.Product?.Discount ?? 0,
                FinalPrice = item.Product?.Price != null
                    ? Math.Round(item.Product.Price * (1 - (item.Product.Discount / 100)), 2)
                    : 0,
                ProductStock = item.Product?.Stock ?? 0,
                ProductHomeImage = !string.IsNullOrWhiteSpace(item.Product?.HomePicture)
                    ? $"https://innova-hub.premiumasp.net{item.Product.HomePicture}"
                    : null,
                ProductPictures = item.Product?.ProductPictures != null
                    ? item.Product.ProductPictures.Select(p => $"https://innova-hub.premiumasp.net{p.PictureUrl}").ToList() // ✅ FIXED HERE
                    : new List<string>(),

            }).ToList();

            return Ok(new { Message = "Wishlist retrieved successfully.", Wishlist = response });
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(
     [FromHeader(Name = "Authorization")] string authorizationHeader, int productId)
        {
            // ✅ Extract user ID from token
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Invalid token or user not found." });
            }

            // ✅ Attempt to remove the product from the wishlist
            var isRemoved = await _unitOfWork.Wishlist.RemoveProductFromWishlist(productId, userId);
            if (!isRemoved)
            {
                return NotFound(new { message = "Product not found in wishlist." });
            }

            return Ok(new { message = "Product removed from wishlist successfully." });
        }

        #region Helper Methods

        #endregion
    }
}
