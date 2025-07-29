using AutoMapper;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _stripeSecretKey;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IUnitOfWork unitOfWork, IConfiguration configuration, IMapper mapper, IConfiguration xconfiguration, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = xconfiguration;
            _logger = logger;
            _stripeSecretKey = _stripeSecretKey = configuration["StripeSettings:SecretKey"];
            StripeConfiguration.ApiKey = _stripeSecretKey;
            _mapper = mapper;
        }

        #region CRUD Endpoints

        [HttpGet("GetAllDeliveryMethod")]
        public async Task<IActionResult> GetDeliveryMethods()
        {
            try
            {
                var deliveryMethods = await _unitOfWork.DeliveryMethod.GetAllAsync();
                var deliveryMethodsDTO = _mapper.Map<List<DeliveryMethodDTO>>(deliveryMethods); // ✅ FIXED MAPPING
                return Ok(deliveryMethodsDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching delivery methods.", Error = ex.Message });
            }
        }

        [HttpPost("Confirm-Order")]
        public async Task<IActionResult> ConfirmOrder(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ConfirmOrderDTO request,
            [FromQuery] int? productId,   // Nullable for "Buy Now"
            [FromQuery] int quantity = 1, // Default quantity for "Buy Now"
            [FromQuery] string platform = "web") // Default to web platform
        {
            try
            {
                // Extract user ID from the token
                var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token or user not found." });

                // Fetch cart items (either from the cart or for "Buy Now")
                List<CartItem> cartItems = await GetCartItems(userId, productId, quantity);

                if (!cartItems.Any())
                    return BadRequest(new { Message = "No items found in the cart." });

                // Validate delivery method
                var deliveryMethod = await _unitOfWork.DeliveryMethod.GetByIdAsync(request.DeliveryMethodId);
                if (deliveryMethod == null)
                    return BadRequest(new { Message = "Invalid shipping method." });

                // Check stock BEFORE creating payment
                foreach (var item in cartItems)
                {
                    if (item.Product.Stock < item.Quantity)
                    {
                        return BadRequest(new { Message = $"Not enough stock for product {item.Product.Name}. Available stock: {item.Product.Stock}" });
                    }
                }

                // Calculate totals
                decimal subtotal = cartItems.Sum(item => item.Product.Price * (1 - item.Product.Discount / 100) * item.Quantity);
                var tax = subtotal * 0.02m; // 2% tax rate
                var totalAmount = subtotal + tax + deliveryMethod.Cost;

                // Handle based on platform
                if (platform.ToLower() == "web")
                {
                    // Create Stripe session for web
                    var session = await CreateStripeSession(
                        cartItems,
                        userId,
                        request.DeliveryMethodId,
                        subtotal,
                        tax,
                        totalAmount,
                        deliveryMethod.Cost,
                        request.UserComment,
                        productId,
                        quantity
                    );

                    return Ok(new
                    {
                        RedirectToCheckoutUrl = session.Url,
                        SessionId = session.Id
                    });
                }
                else if (platform.ToLower() == "mobile" || platform.ToLower() == "flutter")
                {
                    // Create a PaymentIntent for mobile
                    var service = new PaymentIntentService();
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = (long)(totalAmount * 100), // Convert to cents
                        Currency = "usd",
                        PaymentMethodTypes = new List<string> { "card" },
                        Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "TotalAmount", totalAmount.ToString(CultureInfo.InvariantCulture) },
                    { "Subtotal", subtotal.ToString(CultureInfo.InvariantCulture) },
                    { "ShippingCost", deliveryMethod.Cost.ToString(CultureInfo.InvariantCulture) },
                    { "Tax", tax.ToString(CultureInfo.InvariantCulture) },
                    { "DeliveryMethodId", request.DeliveryMethodId.ToString() },
                    { "UserComment", request.UserComment ?? "" },
                    { "OrderDate", DateTime.UtcNow.ToString("o") },
                    { "Platform", "mobile" }
                }
                    };

                    // Add product info for Buy Now
                    if (productId.HasValue)
                    {
                        options.Metadata.Add("ProductId", productId.ToString());
                        options.Metadata.Add("Quantity", quantity.ToString());
                    }

                    try
                    {
                        var intent = await service.CreateAsync(options);

                        // Mobile integration needs the client secret
                        return Ok(new
                        {
                            ClientSecret = intent.ClientSecret,
                            PaymentIntentId = intent.Id,
                            Amount = Math.Round(totalAmount, 2),
                            Currency = "usd",
                            SuccessUrl = _configuration["StripeSettings:Mobile:SuccessUrl"] ?? "innovahub://payment-success",
                            CancelUrl = _configuration["StripeSettings:Mobile:CancelUrl"] ?? "innovahub://payment-failed"
                        });
                    }
                    catch (StripeException ex)
                    {
                        _logger.LogError($"Stripe error creating payment intent: {ex.Message}");
                        return BadRequest(new { Message = "Error creating payment intent", Error = ex.Message });
                    }
                }

                return BadRequest(new { Message = "Invalid platform specified. Use 'web' or 'mobile'." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ConfirmOrder: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred processing your order", Error = ex.Message });
            }
        }

        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentConfirmationRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.SessionId))
                return BadRequest(new { Message = "Session ID is required." });

            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(request.SessionId);

            // Check if payment was successful
            if (session.PaymentStatus != "paid")
                return BadRequest(new { Message = "Payment not successful." });

            var metadata = session.Metadata;
            if (!metadata.TryGetValue("UserId", out var userId) || string.IsNullOrEmpty(userId))
                return BadRequest(new { Message = "Invalid session data." });

            if (!TryExtractOrderDetails(metadata, out var orderDetails))
                return BadRequest(new { Message = "Invalid order details." });

            var shippingAddress = await _unitOfWork.shippingAddress.GetShippingAddressByUserId(userId);
            if (shippingAddress == null)
                return BadRequest(new { Message = "Shipping address not found." });

            List<CartItem> cartItems = new List<CartItem>();

            // ✅ If it's a "Buy Now" purchase, get the product directly
            if (metadata.ContainsKey("ProductId") && int.TryParse(metadata["ProductId"], out var productId))
            {
                var product = await _unitOfWork.Product.GetByIdAsync(productId);
                if (product == null)
                    return BadRequest(new { Message = $"Product {productId} not found." });

                int quantity = int.Parse(metadata["Quantity"]);

                // ✅ Check stock before proceeding
                if (product.Stock < quantity)
                    return BadRequest(new { Message = $"Not enough stock for product {productId}." });

                cartItems.Add(new CartItem { Product = product, Quantity = quantity });
            }
            else
            {
                // If from cart, fetch all cart items
                cartItems = await GetCartItems(userId, null, 0);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            // Create the order and add OrderItems
            var order = await CreateOrder(userId,
                (orderDetails.Total, orderDetails.Subtotal, orderDetails.ShippingCost, orderDetails.Tax, orderDetails.DeliveryMethodId, orderDetails.UserComment),
                session,
                shippingAddress,
                cartItems);

            order.PaymentIntentId = session.PaymentIntentId;
            order.ClientSecret = session.ClientSecret; // Ensure the client secret is populated

            await _unitOfWork.Order.UpdateAsync(order);

            // ✅ Process each product (balance update + stock update)
            foreach (var item in cartItems)
            {
                var author = await _unitOfWork.AppUser.GetUSerByIdAsync(item.Product.AuthorId);
                if (author == null)
                    return BadRequest(new { Message = $"Author for product {item.Product.Id} not found." });

                // ✅ Reduce product stock
                if (item.Product.Stock < item.Quantity)
                    return BadRequest(new { Message = $"Not enough stock for product {item.Product.Id}." });

                item.Product.Stock -= item.Quantity;
                await _unitOfWork.Product.UpdateAsync(item.Product);

                // ✅ Update author's balance
                decimal authorShare = item.Product.Price * item.Quantity * (1 - (item.Product.Discount / 100));
                if (author.TotalAccountBalance == null)
                {
                    author.TotalAccountBalance = 0;
                }
                author.TotalAccountBalance += authorShare;
                await _unitOfWork.AppUser.UpdateAsync(author);

                // Distribute investment profits if applicable
                await DistributeInvestmentProfits(item.Product.Id, authorShare);
            }

            // ✅ Commit stock updates and balance updates at once
            await _unitOfWork.Complete();

            // ✅ Clear the cart if it was a cart purchase
            if (!metadata.ContainsKey("ProductId"))
            {
                var userCart = await _unitOfWork.Cart.GetCartBYUserId(userId);
                if (userCart != null)
                {
                    userCart.CartItems.Clear();
                    await _unitOfWork.Cart.UpdateAsync(userCart);
                }
            }

            await _unitOfWork.Complete();

            // Commit the changes for the transaction
            await transaction.CommitAsync();

            return Ok(new { Message = "Order placed successfully!", OrderId = order.Id });
        }

        [HttpPost("confirm-mobile-payment")]
        public async Task<IActionResult> ConfirmMobilePayment([FromBody] MobilePaymentConfirmationDTO request)
        {
            if (string.IsNullOrEmpty(request.PaymentIntentId))
                return BadRequest(new { Message = "Payment Intent ID is required." });

            try
            {
                // Verificar status de pago
                var paymentIntentService = new PaymentIntentService();
                var intent = await paymentIntentService.GetAsync(request.PaymentIntentId);

                // Registrar para depuración
                _logger.LogInformation($"Payment intent received: {intent.Id}, Status: {intent.Status}");

                // Logea los metadatos de forma más segura
                if (intent.Metadata != null)
                {
                    // Convertimos a string simple para logging
                    string metadataStr = string.Join(", ", intent.Metadata.Select(m => $"{m.Key}={m.Value}"));
                    _logger.LogInformation($"Payment intent metadata: {metadataStr}");
                }
                else
                {
                    _logger.LogInformation("Payment intent metadata is null");
                }

                // PASO 1: Verificar los metadatos
                if (intent.Metadata == null)
                {
                    _logger.LogError("Payment intent metadata is null");
                    return BadRequest(new { Message = "Invalid payment: Missing metadata" });
                }

                // PASO 2: Extraer el ID de usuario
                if (!intent.Metadata.TryGetValue("UserId", out var userId) || string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("UserId not found in metadata");
                    return BadRequest(new { Message = "UserId not found in payment metadata" });
                }

                _logger.LogInformation($"Processing payment for user: {userId}");

                // PASO 3: Extraer detalles del pedido con validación
                bool extractionSuccess = false;
                var orderDetails = default((decimal Total, decimal Subtotal, decimal ShippingCost, decimal Tax, int DeliveryMethodId, string UserComment));

                try
                {
                    extractionSuccess = TryExtractOrderDetails(intent.Metadata, out orderDetails);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting order details");
                }

                if (!extractionSuccess)
                {
                    _logger.LogError("Failed to extract order details from metadata");
                    return BadRequest(new { Message = "Invalid order details in payment metadata" });
                }

                _logger.LogInformation($"Order details extracted: Total={orderDetails.Total}, DeliveryMethodId={orderDetails.DeliveryMethodId}");

                // PASO 4: Obtener dirección de envío
                var shippingAddress = await _unitOfWork.shippingAddress.GetShippingAddressByUserId(userId);
                if (shippingAddress == null)
                {
                    _logger.LogError($"Shipping address not found for user {userId}");
                    return BadRequest(new { Message = "Shipping address not found. Please add a shipping address first." });
                }

                // PASO 5: Obtener elementos del carrito con verificación
                List<CartItem> cartItems = new List<CartItem>();
                try
                {
                    if (intent.Metadata.ContainsKey("ProductId") && int.TryParse(intent.Metadata["ProductId"], out var productId))
                    {
                        if (!intent.Metadata.ContainsKey("Quantity") || !int.TryParse(intent.Metadata["Quantity"], out var quantity))
                        {
                            quantity = 1; // Valor predeterminado
                        }
                        _logger.LogInformation($"Processing 'Buy Now' purchase: ProductId={productId}, Quantity={quantity}");
                        cartItems = await GetCartItems(userId, productId, quantity);
                    }
                    else
                    {
                        _logger.LogInformation($"Processing cart purchase for user {userId}");
                        cartItems = await GetCartItems(userId, null, 0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting cart items");
                    return BadRequest(new { Message = "Error retrieving cart items", Error = ex.Message });
                }

                if (cartItems == null || !cartItems.Any())
                {
                    _logger.LogError("No cart items found to process");
                    return BadRequest(new { Message = "No items found to process. Your cart may be empty." });
                }

                // Verificar validez de los items
                foreach (var item in cartItems)
                {
                    if (item.Product == null)
                    {
                        _logger.LogError($"Cart item has null Product reference");
                        return BadRequest(new { Message = "Invalid cart item: Missing product information" });
                    }
                }

                _logger.LogInformation($"Found {cartItems.Count} items to process");

                // Procesar orden con transacción
                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // PASO 6: Crear orden
                    var order = await CreateOrder(userId,
                        (orderDetails.Total, orderDetails.Subtotal, orderDetails.ShippingCost, orderDetails.Tax, orderDetails.DeliveryMethodId, orderDetails.UserComment),
                        null, // No session object for mobile
                        shippingAddress,
                        cartItems);

                    if (order == null)
                    {
                        _logger.LogError("Failed to create order");
                        return BadRequest(new { Message = "Failed to create order" });
                    }

                    order.PaymentIntentId = intent.Id;
                    order.ClientSecret = intent.ClientSecret;
                    await _unitOfWork.Order.UpdateAsync(order);
                    _logger.LogInformation($"Order created with ID: {order.Id}");

                    // PASO 7: Procesar productos y actualizar inventario
                    foreach (var item in cartItems)
                    {
                        var author = await _unitOfWork.AppUser.GetUSerByIdAsync(item.Product.AuthorId);
                        if (author == null)
                        {
                            _logger.LogError($"Author not found for product {item.Product.Id}");
                            throw new Exception($"Author for product {item.Product.Id} not found.");
                        }

                        // Actualizar inventario
                        item.Product.Stock -= item.Quantity;
                        await _unitOfWork.Product.UpdateAsync(item.Product);
                        _logger.LogInformation($"Updated stock for product {item.Product.Id}. New stock: {item.Product.Stock}");

                        // Actualizar balance del autor
                        decimal authorShare = item.Product.Price * item.Quantity * (1 - (item.Product.Discount / 100));
                        if (author.TotalAccountBalance == null)
                        {
                            author.TotalAccountBalance = 0;
                        }
                        author.TotalAccountBalance += authorShare;
                        await _unitOfWork.AppUser.UpdateAsync(author);
                        _logger.LogInformation($"Updated balance for author {author.Id}. New balance: {author.TotalAccountBalance}");

                        // Distribuir ganancias si corresponde
                        try
                        {
                            await DistributeInvestmentProfits(item.Product.Id, authorShare);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error distributing investment profits for product {item.Product.Id}");
                            // Continue processing - this error shouldn't stop the order
                        }
                    }

                    await _unitOfWork.Complete();
                    _logger.LogInformation("Product processing completed successfully");

                    // PASO 8: Limpiar carrito si es necesario
                    if (!intent.Metadata.ContainsKey("ProductId"))
                    {
                        var userCart = await _unitOfWork.Cart.GetCartBYUserId(userId);
                        if (userCart != null)
                        {
                            userCart.CartItems.Clear();
                            await _unitOfWork.Cart.UpdateAsync(userCart);
                            _logger.LogInformation($"Cart cleared for user {userId}");
                        }
                    }

                    await _unitOfWork.Complete();
                    await transaction.CommitAsync();
                    _logger.LogInformation($"Order transaction committed successfully for order {order.Id}");

                    return Ok(new
                    {
                        Message = "Order placed successfully!",
                        OrderId = order.Id,
                        PaymentStatus = intent.Status
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during order processing");
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogInformation("Transaction rolled back successfully");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Error rolling back transaction");
                    }
                    throw; // Re-throw to be caught by outer catch block
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ConfirmMobilePayment: {ex.Message}");
                return StatusCode(500, new
                {
                    Message = "Error processing payment confirmation",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace // Solo para depuración - eliminar en producción
                });
            }
        }

        // ✅ Payment Failed - Handle failed payment
        [HttpGet("payment-failed")]
        public IActionResult PaymentFailed()
        {
            return BadRequest(new { Message = "Payment failed. Please try again." });
        }

        [HttpPost("webhook/payment")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    "your-webhook-signing-secret"
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    var paymentIntentId = session.PaymentIntentId;
                    var userId = session.Metadata["UserId"];

                    var order = await _unitOfWork.Order.GetByPaymentIntentId(paymentIntentId);
                    if (order != null)
                    {
                        order.OrderStatus = OrderStatus.PaymentReceived;
                        await _unitOfWork.Complete();
                    }

                    return Ok();
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var paymentIntentId = paymentIntent.Id;

                    var order = await _unitOfWork.Order.GetByPaymentIntentId(paymentIntentId);
                    if (order != null)
                    {
                        order.OrderStatus = OrderStatus.PaymentFailed;
                        await _unitOfWork.Complete();
                    }

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Webhook error", Error = ex.Message });
            }

            return BadRequest();
        }

        [HttpPost("return-order")]
        public async Task<IActionResult> ReturnOrder(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ReturnOrderDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Fetch the order by ID
            var order = await _unitOfWork.Order.GetByIdAsync(request.OrderId);
            if (order == null)
                return NotFound(new { Message = "Order not found." });

            // Check if the user is the owner of the order
            if (order.UserId != userId)
                return Unauthorized(new { Message = "User does not have permission to return this order." });

            // Check if the order is eligible for return (e.g., within a return window)
            if (order.ReturnStatus != ReturnStatus.None)
                return BadRequest(new { Message = "This order has already been returned or is in process." });

            // Start the return process (set status to Pending)
            order.ReturnStatus = ReturnStatus.Pending;
            await _unitOfWork.Complete();

            // Optionally, log the return request
            var returnRequest = new OrderReturnRequest
            {
                OrderId = order.Id,
                UserId = userId,
                ReturnReason = request.ReturnReason,
                RequestedAt = DateTime.UtcNow
            };
            await _unitOfWork.OrderReturnRequest.AddAsync(returnRequest);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Return request submitted. Awaiting approval." });
        }

        [HttpPost("approve-return")]
        public async Task<IActionResult> ApproveReturn(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ApproveReturnDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Ensure user is an admin
            var isAdmin = await _unitOfWork.Auth.IsAdmin(userId);
            if (!isAdmin)
                return Unauthorized(new { Message = "User does not have admin privileges." });

            var order = await _unitOfWork.Order.GetByIdAsync(request.OrderId);
            if (order == null)
                return NotFound(new { Message = "Order not found." });

            // Approve the return and mark order status
            order.ReturnStatus = ReturnStatus.Approved;
            await _unitOfWork.Complete();

            // Refund the user via Stripe
            var paymentIntentService = new PaymentIntentService();
            try
            {
                var paymentIntent = await paymentIntentService.GetAsync(order.PaymentIntentId);

                // Create the refund request
                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = order.PaymentIntentId,
                    Amount = (long)(order.Total * 100) // Convert to cents
                };
                var refund = await refundService.CreateAsync(refundOptions);

                // Optionally, log the refund
                var refundLog = new InnoHub.Core.Models.PaymentRefundLog
                {
                    OrderId = order.Id,
                    RefundAmount = order.Total,
                    RefundId = refund.Id,
                    RefundStatus = refund.Status,
                    RefundCreated = DateTime.UtcNow
                };
                await _unitOfWork.PaymentRefundLog.AddAsync(refundLog);
                await _unitOfWork.Complete();

                // Finalize the order
                order.ReturnStatus = ReturnStatus.Completed;
                await _unitOfWork.Complete();

                return Ok(new { Message = "Return approved and refund processed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error processing refund.", Error = ex.Message });
            }
        }

        [HttpPost("reject-return")]
        public async Task<IActionResult> RejectReturn(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] RejectReturnDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Ensure user is an admin
            var isAdmin = await _unitOfWork.Auth.IsAdmin(userId);
            if (!isAdmin)
                return Unauthorized(new { Message = "User does not have admin privileges." });

            var order = await _unitOfWork.Order.GetByIdAsync(request.OrderId);
            if (order == null)
                return NotFound(new { Message = "Order not found." });

            // Reject the return request
            order.ReturnStatus = ReturnStatus.Rejected;
            await _unitOfWork.Complete();

            return Ok(new { Message = "Return request rejected." });
        }
        #endregion

        #region Helper Methods
        private async Task DistributeInvestmentProfits(int productId, decimal productProfit)
        {
            // Check if this product is linked to an deal
            var deal = await _unitOfWork.Deal.GetDealsByProductId(productId);
            if (deal == null || deal.Status != DealStatus.Active)
                return; // No active deals for this product

            // Calculate profit shares based on the deal agreement
            decimal investorShare = productProfit * (deal.OfferDeal / 100);
            decimal platformFee = investorShare * (deal.PlatformFeePercentage / 100);
            decimal ownerShare = productProfit - investorShare;

            // Calculate net profit for this period
            var currentMonth = DateTime.UtcNow.ToString("MMM yyyy");

            // Create or update profit distribution record for this month
            var existingDistribution = await _unitOfWork.InvestmentProfit
                .GetProfitDistributionForPeriod(deal.Id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            if (existingDistribution != null)
            {
                // Update existing distribution
                existingDistribution.NetProfit += productProfit;
                existingDistribution.InvestorShare += investorShare - platformFee;
                existingDistribution.OwnerShare += ownerShare;
                existingDistribution.PlatformFee += platformFee;

                await _unitOfWork.InvestmentProfit.UpdateAsync(existingDistribution);
            }
            else
            {
                // Create new distribution
                var profitDistribution = new DealProfit
                {
                    DealId = deal.Id,
                    NetProfit = productProfit,
                    InvestorShare = investorShare - platformFee,
                    OwnerShare = ownerShare,
                    PlatformFee = platformFee,
                    DistributionDate = DateTime.UtcNow,
                    IsPaid = false,
                    TotalRevenue = productProfit, // Using profit as revenue since we don't have actual sales data
                    ManufacturingCost = 0,        // These costs are already accounted for in profit calculation
                    OtherCosts = 0
                };

                await _unitOfWork.InvestmentProfit.AddAsync(profitDistribution);
            }

            await _unitOfWork.Complete();
        }

        private async Task<List<CartItem>> GetCartItems(string userId, int? productId, int quantity)
        {
            if (productId.HasValue)
            {
                // "Buy Now": Process a single product
                var product = await _unitOfWork.Product.GetByIdAsync(productId.Value);
                if (product == null)
                    return new List<CartItem>();  // No product found

                return new List<CartItem> { new CartItem { Product = product, Quantity = Math.Max(quantity, 1) } };
            }

            // "Buy from Cart": Fetch all items from user's cart
            var cart = await _unitOfWork.Cart.GetCartBYUserId(userId);
            return cart?.CartItems.ToList() ?? new List<CartItem>();
        }

        private async Task<Session> CreateStripeSession(
            List<CartItem> cartItems,
            string userId,
            int deliveryMethodId,
            decimal subtotal,
            decimal tax,
            decimal totalAmount,
            decimal shippingCost,
            string? userComment,
            int? productId,
            int quantity)
        {
            try
            {
                var sessionService = new SessionService();

                // Create line items for products
                var lineItems = new List<SessionLineItemOptions>();

                if (productId.HasValue)
                {
                    // Single product checkout (Buy Now)
                    var product = cartItems.FirstOrDefault()?.Product;
                    if (product != null)
                    {
                        lineItems.Add(new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(product.Price * (1 - product.Discount / 100) * 100), // Apply discount
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = product.Name,
                                    Description = product.Description?.Substring(0, Math.Min(product.Description?.Length ?? 0, 50)) ?? "Product",
                                    Images = !string.IsNullOrEmpty(product.HomePicture)
                                        ? new List<string> { $"https://innova-hub.premiumasp.net{product.HomePicture}" }
                                        : null
                                }
                            },
                            Quantity = quantity
                        });
                    }

                    // Add shipping as a separate line item
                    if (shippingCost > 0)
                    {
                        lineItems.Add(new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(shippingCost * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Shipping",
                                    Description = "Shipping and handling"
                                }
                            },
                            Quantity = 1
                        });
                    }

                    // Add tax as a separate line item
                    if (tax > 0)
                    {
                        lineItems.Add(new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(tax * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Tax",
                                    Description = "Sales tax"
                                }
                            },
                            Quantity = 1
                        });
                    }
                }
                else
                {
                    // Full cart checkout - simplified to a single line item for order total
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(totalAmount * 100), // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Order Total",
                                Description = $"Subtotal: ${subtotal:F2}, Tax: ${tax:F2}, Shipping: ${shippingCost:F2}"
                            }
                        },
                        Quantity = 1
                    });
                }

                // Create metadata to track order details
                var metadata = new Dictionary<string, string>
        {
            { "UserId", userId },
            { "TotalAmount", totalAmount.ToString(CultureInfo.InvariantCulture) },
            { "Subtotal", subtotal.ToString(CultureInfo.InvariantCulture) },
            { "ShippingCost", shippingCost.ToString(CultureInfo.InvariantCulture) },
            { "Tax", tax.ToString(CultureInfo.InvariantCulture) },
            { "DeliveryMethodId", deliveryMethodId.ToString() },
            { "UserComment", userComment ?? "" },
            { "OrderDate", DateTime.UtcNow.ToString("o") },
            { "Platform", "web" }
        };

                // Add Buy Now details if applicable
                if (productId.HasValue)
                {
                    metadata.Add("ProductId", productId.ToString());
                    metadata.Add("Quantity", quantity.ToString());
                }

                // Get success and cancel URLs from configuration
                string successUrl = _configuration["StripeSettings:Web:SuccessUrl"] ??
                                   "http://localhost:5173/order/payment-success?session_id={{CHECKOUT_SESSION_ID}}";

                string cancelUrl = _configuration["StripeSettings:Web:CancelUrl"] ??
                                  "http://localhost:5173/order/payment-failed";

                // Configuration for session
                var sessionOptions = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = metadata,
                    CustomerEmail = cartItems.FirstOrDefault()?.Product?.Author?.Email, // Pre-fill customer email if available
                    Locale = "en", // Set locale
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        CaptureMethod = "automatic"
                    },
                    BillingAddressCollection = "required", // Collect billing address
                    ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                    {
                        AllowedCountries = new List<string> { "US" } // Add more countries as needed
                    }
                };

                // Create the session
                var session = await sessionService.CreateAsync(sessionOptions);

                // Log the session creation for troubleshooting
                _logger.LogInformation($"Stripe session created: {session.Id} for user {userId}");

                return session;
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe error creating session: {ex.Message}");
                throw new InvalidOperationException($"An error occurred while creating the Stripe session: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Stripe session: {ex.Message}");
                throw new InvalidOperationException($"An error occurred while creating the Stripe session", ex);
            }
        }

        private bool TryExtractOrderDetails(
            Dictionary<string, string> metadata,
            out (decimal Total, decimal Subtotal, decimal ShippingCost, decimal Tax, int DeliveryMethodId, string? UserComment) details)
        {
            details = default;

            if (!decimal.TryParse(metadata.GetValueOrDefault("TotalAmount"), out var total) ||
                !decimal.TryParse(metadata.GetValueOrDefault("Subtotal"), out var subtotal) ||
                !decimal.TryParse(metadata.GetValueOrDefault("ShippingCost"), out var shippingCost) ||
                !int.TryParse(metadata.GetValueOrDefault("DeliveryMethodId"), out var deliveryMethodId))
            {
                return false;
            }

            // Extract Tax safely (default to 0 if missing)
            decimal tax = 0m;
            if (metadata.ContainsKey("Tax") && decimal.TryParse(metadata["Tax"], out var parsedTax))
            {
                tax = parsedTax;
            }

            details = (total, subtotal, shippingCost, tax, deliveryMethodId, metadata.GetValueOrDefault("UserComment"));
            return true;
        }

        private async Task<InnoHub.Core.Models.Order> CreateOrder(
    string userId,
    (decimal Total, decimal Subtotal, decimal ShippingCost, decimal Tax, int DeliveryMethodId, string UserComment) orderDetails,
    Session session,
    ShippingAddress shippingAddress,
    List<CartItem> cartItems)
        {
            var order = new InnoHub.Core.Models.Order
            {
                UserId = userId,
                Total = orderDetails.Total,
                Subtotal = orderDetails.Subtotal,
                ShippingCost = orderDetails.ShippingCost,
                Tax = orderDetails.Tax,
                OrderDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.PaymentReceived,
                PaymentIntentId = session?.PaymentIntentId ?? "", // استخدام معامل الوصول الشرطي
                DeliveryMethodId = orderDetails.DeliveryMethodId,
                UserComment = orderDetails.UserComment ?? "",
                ShippingAddressId = shippingAddress.Id,
                OrderItems = new List<OrderItem>(),
                ClientSecret = session?.ClientSecret ?? "", // استخدام معامل الوصول الشرطي
            };

            // إضافة عناصر الطلب من عناصر السلة
            foreach (var cartItem in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.Product.Id,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price
                });
            }

            await _unitOfWork.Order.AddAsync(order);
            await _unitOfWork.Complete();
            return order;
        }
        #endregion
    }
}