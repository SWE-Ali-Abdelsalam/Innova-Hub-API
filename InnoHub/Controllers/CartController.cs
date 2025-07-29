using AutoMapper;
using Azure.Core;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartController> _logger;
        private readonly IMapper _mapper;

        public CartController(IUnitOfWork unitOfWork, ILogger<CartController> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] AddToCartDTO cartDTO)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            try
            {
                var cart = await _unitOfWork.Cart.CreateCart(userId, cartDTO.ProductId, cartDTO.Quantity);
                if (cart == null)
                    return BadRequest(new { Message = $"Failed to add product {cartDTO.ProductId} to cart for user {userId}" });

                return Ok(new { Message = "Product added to cart successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {cartDTO.ProductId} to cart for user {userId}");
                return BadRequest(new { Message = "Error while adding product to cart." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCart([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Extract user ID from the token
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Retrieve the cart for the user
            var cart = await _unitOfWork.Cart.GetCartBYUserId(userId);
            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                return Ok(new { Message = "Cart is empty.", Cart = Array.Empty<object>() });

            // Get all product IDs from the cart
            var productIds = cart.CartItems.Select(item => item.ProductId).Distinct();

            // Fetch product details for these product IDs (including ProductPictures)
            var products = await _unitOfWork.Product.GetProductsByIdsAsync(productIds);

            // Create a lookup dictionary for quick access to product details
            var productDetails = products.ToDictionary(
                product => product.Id,
                product => new
                {
                    product.Name,
                    product.HomePicture,
                    product.ProductPictures
                });

            // Map cart items to DTO
            var cartItems = cart.CartItems.Select(item =>
            {
                var productDetail = productDetails.GetValueOrDefault(item.ProductId);

                return new CartItemDTO
                {
                    ProductOwnerId = item.Product.Author.Id,  // Handle null Author
                    ProductOwnerName = item.Product.Author != null
                        ? $"{item.Product.Author.FirstName} {item.Product.Author.LastName}"
                        : "Unknown Author",  // Ensure null safety for Author
                    ProductId = item.ProductId,
                    ProductName = productDetail?.Name ?? "Unknown Product",
                    Quantity = item.Quantity,
                    Price = item.Price,
                    HomePictureUrl = productDetail?.HomePicture != null
                        ? $"https://innova-hub.premiumasp.net{productDetail.HomePicture}"
                        : null,
                    ProductPictures = productDetail?.ProductPictures?.Select(pic =>
                        $"https://innova-hub.premiumasp.net{pic.PictureUrl}").ToList()
                                       ?? new List<string>() // ✅ FIXED: Default to empty list if null
                };
            }).ToList();

            // Ensure cart.User is not null before accessing FirstName & LastName
            var CartAuthorName = cart.User.FirstName + " " + cart.User.LastName ;
            // Create the response DTO
            var response = new AddToCartResponseDTO
            {
                TotalPrice = cart.TotalPrice,
                CartAuthorId = userId,
                CartAuthorName = CartAuthorName,
                NumberOfProducts = cartItems.Count,
                cartItems = cartItems  // Ensure the naming is consistent with the DTO property
            };

            return Ok(response);
        }

        [HttpPatch("update-quantity")]
        public async Task<IActionResult> UpdateCartItemQuantity(
      [FromHeader(Name = "Authorization")] string authorizationHeader,
      [FromBody] UpdateQuantityDTO request)
        {
            // Extract user ID from the token
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var result = await _unitOfWork.Cart.UpdateProductQuantity(userId, request.ProductId, request.quantity);

            if (result == null)
                return BadRequest(new { Message = "Product not found in cart or requested quantity exceeds stock." });

            return Ok(new { Message = "Cart item quantity updated successfully." });
        }
      
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart([FromHeader(Name = "Authorization")] string authorizationHeader, [FromRoute] int productId)
        {
            // Extract user ID from the token
            var userId =_unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Invalid token or user not found." });
            }

            try
            {
                // Check if the product exists in the user's cart
                var productExists = await _unitOfWork.Cart.CheckIfProductExistsInCart(userId, productId);
                if (!productExists)
                {
                    return NotFound(new { Message = $"Product with ID {productId} does not exist in the cart." });
                }

                // Remove the product from the cart
                var cart = await _unitOfWork.Cart.DeleteProductFromCart(userId, productId);
                if (cart == null || cart.CartItems.Any(i => i.ProductId == productId))
                {
                    return BadRequest(new { Message = $"Failed to remove product with ID {productId} from the cart." });
                }

                return Ok(new { Message = "Product removed from cart successfully."});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cart for user {UserId}", productId, userId);
                return BadRequest(new { Message = "Error while removing product from cart." });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Extract user ID from the token
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var result = await _unitOfWork.Cart.ClearCart(userId);
            if (result == null)
                return BadRequest(new { Message = "Cart is already empty or does not exist." });

            return Ok(new { Message = "Cart cleared successfully." });
        }
   
        #region Helper Methods
        #endregion
    }
}
