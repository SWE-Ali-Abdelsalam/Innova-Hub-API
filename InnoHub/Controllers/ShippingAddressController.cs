using AutoMapper;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingAddressController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ShippingAddressController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // ✅ Add Shipping Address
        [HttpPost("add")]
        public async Task<IActionResult> AddShippingAddress(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ShippingAddressDTO addressDto)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var existingAddress = await _unitOfWork.shippingAddress.GetShippingAddressByUserId(userId);
            if (existingAddress != null)
                return BadRequest(new { Message = "User already has a shipping address. Use update instead." });

            var newAddress = new ShippingAddress
            {
                UserId = userId,
                FirstName = addressDto.FirstName,
                LastName = addressDto.LastName,
                StreetAddress = addressDto.StreetAddress,
                Apartment = addressDto.Apartment,
                Email = addressDto.Email,
                Phone = addressDto.Phone,
                City = addressDto.City,
                ZipCode = addressDto.ZipCode
            };

            await _unitOfWork.shippingAddress.AddAsync(newAddress);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Shipping address added successfully." });
        }

        // ✅ Update Shipping Address
        [HttpPatch("update")]
        public async Task<IActionResult> UpdateShippingAddress(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ShippingAddressDTO addressDto)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var existingAddress = await _unitOfWork.shippingAddress.GetShippingAddressByUserId(userId);
            if (existingAddress == null)
                return NotFound(new { Message = "Shipping address not found. Please add an address first." });

            existingAddress.FirstName = addressDto.FirstName;
            existingAddress.LastName = addressDto.LastName;
            existingAddress.StreetAddress = addressDto.StreetAddress;
            existingAddress.Apartment = addressDto.Apartment;
            existingAddress.Email = addressDto.Email;
            existingAddress.Phone = addressDto.Phone;
            existingAddress.City = addressDto.City;
            existingAddress.ZipCode = addressDto.ZipCode;

            await _unitOfWork.shippingAddress.UpdateAsync(existingAddress);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Shipping address updated successfully." });
        }

        // ✅ Get Shipping Address
        [HttpGet]
        public async Task<IActionResult> GetShippingAddress(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var shippingAddress = await _unitOfWork.shippingAddress.GetShippingAddressByUserId(userId);
            if (shippingAddress == null)
                return NotFound(new { Message = "No shipping address found for this user." });
            var mappedShipping = _mapper.Map<ShippingAddressDTO>(shippingAddress);
            return Ok(mappedShipping);
        }

        // ✅ Check if User Has a Shipping Address
        [HttpGet("exists")]
        public async Task<IActionResult> CheckShippingAddress(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            bool hasAddress = await _unitOfWork.shippingAddress.UserHasShippingAddress(userId);
            return Ok(new { HasAddress = hasAddress });
        }
        // ✅ Get Order Summary (Buy Now & Cart Checkout)
       
        
        [HttpPost("order-summary")]
        public async Task<IActionResult> GetOrUpdateOrderSummary(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromQuery] int? productId,
    [FromQuery] int? quantity,
    [FromBody] UpdateShippingDTO? request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            decimal subtotal = 0m, tax = 0m, shippingCost = 0m;

            // "Buy Now" scenario
            if (productId.HasValue && quantity.HasValue)
            {
                var product = await _unitOfWork.Product.GetByIdAsync(productId.Value);
                if (product == null)
                    return BadRequest(new { Message = "Product not found." });

                if (quantity.Value <= 0)
                    return BadRequest(new { Message = "Quantity must be at least 1." });

                if (quantity.Value > product.Stock)
                    return BadRequest(new { Message = $"Quantity cannot be more than the product stock {product.Stock}." });

                // Apply discount if available
                decimal discountedPrice = product.Price * (1 - (product.Discount / 100));

                subtotal = Math.Round(quantity.Value * discountedPrice, 2);
            }
            else // "Cart Checkout" scenario
            {
                var cart = await _unitOfWork.Cart.GetCartBYUserId(userId);
                if (cart == null || !cart.CartItems.Any())
                    return NotFound(new { Message = "Cart is empty." });

                if (cart.CartItems.Any(item => item.Product == null))
                    return BadRequest(new { Message = "Some products in the cart are missing or invalid." });

                // Apply discount to each product in the cart
                subtotal = Math.Round(cart.CartItems.Sum(item =>
                    item.Quantity * (item.Product.Price * (1 - (item.Product.Discount / 100)))), 2);
            }

            tax = Math.Round(subtotal * 0.02m, 2);

            if (request != null && request.DeliveryMethodId > 0)
            {
                var deliveryMethod = await _unitOfWork.DeliveryMethod.GetByIdAsync(request.DeliveryMethodId);
                if (deliveryMethod == null)
                    return BadRequest(new { Message = "Invalid delivery method." });

                shippingCost = Math.Round(deliveryMethod.Cost, 2);
                decimal totalWithShipping = Math.Round(subtotal + tax + shippingCost, 2);

                return Ok(new OrderSummaryDTO
                {
                    Subtotal = subtotal,
                    ShippingDeliveryMethod = shippingCost,
                    Taxes = tax,
                    Total = totalWithShipping
                });
            }

            decimal totalWithoutShipping = Math.Round(subtotal + tax, 2);
            return Ok(new OrderSummaryDTO
            {
                Subtotal = subtotal,
                ShippingDeliveryMethod = 0m,
                Taxes = tax,
                Total = totalWithoutShipping
            });
        }

    }
}
