using AutoMapper;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Cms;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Globalization;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _stripeSecretKey;
        private readonly string _stripeWebhookSecret;
        private readonly decimal _platformFeePercentage;
        private readonly string _webBaseUrl;
        private readonly string _mobileDeepLinkPrefix;
        private const string _adminUserId = "bba1816c-b7a5-49cb-b282-9895bffde438";

        public PaymentController(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PaymentController> logger, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _stripeSecretKey = configuration["StripeSettings:SecretKey"]!;
            StripeConfiguration.ApiKey = _stripeSecretKey;
            _stripeWebhookSecret = configuration["StripeSettings:WebhookSecret"]!;
            _platformFeePercentage = decimal.Parse(configuration["StripeSettings:PlatformFeePercentage"] ?? "1.0");
            _webBaseUrl = configuration["ClientBaseUrl"]!;
            _mobileDeepLinkPrefix = configuration["MobileSettings:DeepLinkPrefix"]!;
        }

        #region CRUD Endpoints

        [HttpPost("process-web-payment")]
        public async Task<IActionResult> ProcessWebPayment(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ProcessPaymentDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Verify this is the investor
            if (deal.InvestorId != userId)
                return Unauthorized(new { Message = "Only the investor can process payment for this deal." });

            if (deal.Status != DealStatus.AdminApproved)
                return BadRequest(new { Message = "Deal status is not suitable for this process." });

            if (deal.IsPaymentProcessed)
                return BadRequest(new { Message = "Payment has already been processed for this deal." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            bool isUserVerified = currentUser != null && currentUser.IsIdCardVerified;
            // Check ID verification status
            if (!isUserVerified)
                return BadRequest(new
                {
                    Message = "Your ID card must be verified before accepting deal offers.",
                    VerificationStatus = "unverified"
                });

            if (!deal.Author.IsSignatureVerified || !deal.Investor.IsSignatureVerified)
                return BadRequest(new { Message = "Both investor and business owner must upload their signatures." });

            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // Create success and cancel URLs for web
                var baseUrl = _configuration["ClientBaseUrl"] ?? "https://innova-hub.premiumasp.net";
                var successUrl = $"{baseUrl}/api/Payment/payment-success";
                var cancelUrl = $"{baseUrl}/api/Payment/payment-cancel";

                // Create Stripe checkout session (web-specific)
                var session = await CreateDealCheckoutSession(
                    deal, successUrl, cancelUrl);//

                // Update deal with payment intent ID
                deal.PaymentIntentId = session.PaymentIntentId;
                deal.PaymentStatus = "pending";
                deal.Platform = "web";
                deal.DurationInMonths = request.DurationInMonths;

                await _unitOfWork.Deal.UpdateAsync(deal);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Payment session created successfully.",
                    SessionId = session.Id,
                    PaymentUrl = session.Url
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating web payment session for deal {deal.Id}");

                return StatusCode(500, new { Message = "Error processing payment", Error = ex.Message });
            }
        }

        [HttpPost("process-mobile-payment")]
        public async Task<IActionResult> ProcessMobilePayment(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ProcessPaymentDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Verify this is the investor
            if (deal.InvestorId != userId)
                return Unauthorized(new { Message = "Only the investor can process payment for this deal." });

            if (deal.Status != DealStatus.AdminApproved)
                return BadRequest(new { Message = "Deal status is not suitable for this process." });

            if (deal.IsPaymentProcessed)
                return BadRequest(new { Message = "Payment has already been processed for this deal." });

            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // Create payment intent for mobile SDK
                var (clientSecret, paymentIntentId) = await CreateDealPaymentIntent(deal);

                // Update deal with payment intent details
                deal.PaymentIntentId = paymentIntentId;
                deal.PaymentClientSecret = clientSecret;
                deal.PaymentStatus = "pending";
                deal.Platform = "mobile";
                deal.DurationInMonths = request.DurationInMonths;

                await _unitOfWork.Deal.UpdateAsync(deal);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Payment intent created successfully.",
                    ClientSecret = clientSecret,
                    PaymentIntentId = paymentIntentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating mobile payment intent for deal {deal.Id}");

                return StatusCode(500, new { Message = "Error processing payment", Error = ex.Message });
            }
        }

        [HttpPost("confirm-mobile-payment")]
        public async Task<IActionResult> ConfirmMobilePayment(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ConfirmMobilePaymentDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetByPaymentIntentId(request.PaymentIntentId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Verify this is the investor
            if (deal.InvestorId != userId)
                return Unauthorized(new { Message = "Only the investor can confirm payment for this deal." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            bool isUserVerified = currentUser != null && currentUser.IsIdCardVerified;
            // Check ID verification status
            if (!isUserVerified)
                return BadRequest(new
                {
                    Message = "Your ID card must be verified before accepting deal offers.",
                    VerificationStatus = "unverified"
                });

            if (!deal.Author.IsSignatureVerified || !deal.Investor.IsSignatureVerified)
                return BadRequest(new { Message = "Both investor and business owner must upload their signatures." });

            if (deal.Platform != "mobile")
                return BadRequest(new { Message = "Use the endpoint process-mobile-payment firstly." });

            // Update deal payment status
            deal.IsPaymentProcessed = true;
            deal.PaymentProcessedAt = DateTime.UtcNow;
            deal.PaymentStatus = "completed";

            // Create a new product from this deal
            var product = new InnoHub.Core.Models.Product
            {
                Name = deal.BusinessName,
                Description = deal.Description,
                HomePicture = deal.Pictures.FirstOrDefault() ?? "",
                AuthorId = deal.AuthorId!,
                CategoryId = deal.CategoryId,
                Price = deal.EstimatedPrice,
                ProductPictures = ConvertDealPicturesToProductPictures(deal.Pictures)
            };

            // Add the product to the database
            await _unitOfWork.Product.AddAsync(product);

            // Update the deal with the product reference
            deal.ProductId = product.Id;
            deal.IsProductCreated = true;

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            try
            {
                await GenerateContractDocument(deal);

                // Notify parties about contract ready for signing
                await NotifyPartiesAboutContractGeneration(deal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating contract for deal {deal.Id}");
            }

            await _unitOfWork.Deal.UpdateAsync(deal);

            // Notify the business owner
            var ownerNotification = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = deal.AuthorId,
                MessageText = $"Payment for the deal in '{deal.BusinessName}' has been processed successfully. " +
                              $"Amount: {deal.OfferMoney:C}. Please confirm to complete the deal.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            // Notify the investor
            var investorNotification = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = deal.InvestorId,
                MessageText = $"Your payment for the deal in '{deal.BusinessName}' has been processed successfully. " +
                              $"Amount: {deal.OfferMoney:C}. Waiting for business owner confirmation.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
            await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = "Payment confirmed successfully.",
                ContractUrl = deal.ContractDocumentUrl
            });
        }

        // ========== Change Payment Methods ==========

        [HttpPost("process-change-payment")]
            public async Task<IActionResult> ProcessChangePayment(
                [FromHeader(Name = "Authorization")] string authorizationHeader,
                [FromBody] ProcessChangePaymentDTO request)
            {
                var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token or user not found." });

                var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
                if (deal == null)
                    return NotFound(new { Message = "Deal not found." });

                var changeRequest = await _unitOfWork.DealChangeRequest.GetWithDetailsAsync(request.ChangeRequestId);
                if (changeRequest == null)
                    return NotFound(new { Message = "Change request not found." });

                // التحقق من أن الطلب مقبول
                if (changeRequest.Status != ChangeRequestStatus.Approved)
                    return BadRequest(new { Message = "Change request must be approved before payment processing." });

                // منع تكرار الدفع
                if (deal.IsChangePaymentProcessed)
                    return BadRequest(new { Message = "Change payment has already been processed." });

                try
                {
                    // حساب الفرق في المبلغ
                    var originalValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                        changeRequest.OriginalValues, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var requestedValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                        changeRequest.RequestedValues, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    decimal originalAmount = GetDecimalFromJson(originalValues, "OfferMoney") ?? deal.OfferMoney;
                    decimal newAmount = GetDecimalFromJson(requestedValues, "OfferMoney") ?? deal.OfferMoney;
                    decimal amountDifference = newAmount - originalAmount;

                    // إذا لا يوجد تغيير في المبلغ، فقط إنشاء عقد جديد
                    if (Math.Abs(amountDifference) < 0.01m)
                    {
                        await ApplyChangesToDeal(deal, changeRequest);
                        await GenerateChangeContract(deal);

                        deal.IsChangePaymentProcessed = true;
                        deal.ChangePaymentProcessedAt = DateTime.UtcNow;
                        deal.ChangeRequestId = request.ChangeRequestId;

                        await _unitOfWork.Deal.UpdateAsync(deal);
                        await _unitOfWork.Complete();

                        await SendChangeCompletedNotifications(deal, false, 0);

                        return Ok(new ChangePaymentResponseDTO
                        {
                            Message = "Changes applied successfully. New contract generated.",
                            RequiresPayment = false,
                            ChangeRequestId = request.ChangeRequestId
                        });
                    }

                    // التحقق من هوية الدافع
                    bool shouldInvestorPay = amountDifference > 0;
                    string expectedPayerId = shouldInvestorPay ? deal.InvestorId : deal.AuthorId;

                    if (userId != expectedPayerId)
                    {
                        string requiredRole = shouldInvestorPay ? "investor" : "business owner";
                        return Unauthorized(new { Message = $"Only the {requiredRole} can process this payment." });
                    }

                    StripeConfiguration.ApiKey = _stripeSecretKey;
                    decimal absoluteAmount = Math.Abs(amountDifference);

                    // إنشاء hash لمنع تكرار العملية
                    string paymentHash = GeneratePaymentHash(deal.Id, "change", absoluteAmount, userId);

                    if (deal.LastProcessedPaymentHash == paymentHash)
                        return BadRequest(new { Message = "This payment has already been processed." });

                    if (shouldInvestorPay)
                    {
                        // المستثمر يدفع الزيادة
                        if (request.Platform.ToLower() == "web")
                        {
                            var session = await CreateChangeCheckoutSession(deal, changeRequest, absoluteAmount, "additional_investment");

                            deal.ChangePaymentIntentId = session.PaymentIntentId;
                            deal.IsChangePaymentRequired = true;
                            deal.ChangeAmountDifference = amountDifference;
                            deal.ChangeRequestId = request.ChangeRequestId;
                            deal.LastProcessedPaymentHash = paymentHash;

                            await _unitOfWork.Deal.UpdateAsync(deal);
                            await _unitOfWork.Complete();

                            return Ok(new ChangePaymentResponseDTO
                            {
                                Message = "Additional payment required from investor.",
                                RequiresPayment = true,
                                PaymentAmount = absoluteAmount,
                                PaymentDirection = "investor_pays",
                                PaymentUrl = session.Url,
                                ChangeRequestId = request.ChangeRequestId
                            });
                        }
                        else // mobile
                        {
                            var (clientSecret, paymentIntentId) = await CreateChangePaymentIntent(deal, changeRequest, absoluteAmount, "additional_investment");

                            deal.ChangePaymentIntentId = paymentIntentId;
                            deal.IsChangePaymentRequired = true;
                            deal.ChangeAmountDifference = amountDifference;
                            deal.ChangeRequestId = request.ChangeRequestId;
                            deal.LastProcessedPaymentHash = paymentHash;

                            await _unitOfWork.Deal.UpdateAsync(deal);
                            await _unitOfWork.Complete();

                            return Ok(new ChangePaymentResponseDTO
                            {
                                Message = "Additional payment required from investor.",
                                RequiresPayment = true,
                                PaymentAmount = absoluteAmount,
                                PaymentDirection = "investor_pays",
                                ClientSecret = clientSecret,
                                PaymentIntentId = paymentIntentId,
                                ChangeRequestId = request.ChangeRequestId
                            });
                        }
                    }
                    else
                    {
                        // رد أموال للمستثمر (صاحب العمل يدفع)
                        await ProcessRefundToInvestor(deal, absoluteAmount, changeRequest);

                        deal.IsChangePaymentProcessed = true;
                        deal.ChangePaymentProcessedAt = DateTime.UtcNow;
                        deal.ChangeAmountDifference = amountDifference;
                        deal.ChangeRequestId = request.ChangeRequestId;
                        deal.LastProcessedPaymentHash = paymentHash;

                        await _unitOfWork.Deal.UpdateAsync(deal);
                        await _unitOfWork.Complete();

                        return Ok(new ChangePaymentResponseDTO
                        {
                            Message = "Refund processed to investor successfully.",
                            RequiresPayment = false,
                            PaymentAmount = absoluteAmount,
                            PaymentDirection = "refund_to_investor",
                            ChangeRequestId = request.ChangeRequestId
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing change payment for deal {request.DealId}");
                    return StatusCode(500, new { Message = "Error processing change payment", Error = ex.Message });
                }
            }

            [HttpPost("confirm-change-payment")]
            public async Task<IActionResult> ConfirmChangePayment(
                [FromHeader(Name = "Authorization")] string authorizationHeader,
                [FromBody] ConfirmChangePaymentDTO request)
            {
                var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "Invalid token or user not found." });

                var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
                if (deal == null)
                    return NotFound(new { Message = "Deal not found." });

                var changeRequest = await _unitOfWork.DealChangeRequest.GetWithDetailsAsync(request.ChangeRequestId);
                if (changeRequest == null)
                    return NotFound(new { Message = "Change request not found." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            bool isUserVerified = currentUser != null && currentUser.IsIdCardVerified;
            // Check ID verification status
            if (!isUserVerified)
                return BadRequest(new
                {
                    Message = "Your ID card must be verified before accepting deal offers.",
                    VerificationStatus = "unverified"
                });

            if (!deal.Author.IsSignatureVerified || !deal.Investor.IsSignatureVerified)
                return BadRequest(new { Message = "Both investor and business owner must upload their signatures." });

            try
                {
                    StripeConfiguration.ApiKey = _stripeSecretKey;
                    var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                    bool paymentSuccessful = false;

                    if (!string.IsNullOrEmpty(request.SessionId))
                    {
                        var sessionService = new SessionService(stripeClient);
                        var session = await sessionService.GetAsync(request.SessionId);
                        paymentSuccessful = session.PaymentStatus == "paid";
                    }
                    else if (!string.IsNullOrEmpty(request.PaymentIntentId))
                    {
                        var paymentIntentService = new PaymentIntentService(stripeClient);
                        var paymentIntent = await paymentIntentService.GetAsync(request.PaymentIntentId);
                        paymentSuccessful = paymentIntent.Status == "succeeded";
                    }
                    else
                    {
                        return BadRequest(new { Message = "Either SessionId or PaymentIntentId is required." });
                    }

                    if (paymentSuccessful)
                    {
                        // تطبيق التغييرات
                        await ApplyChangesToDeal(deal, changeRequest);

                        // إنشاء عقد جديد
                        await GenerateChangeContract(deal);

                        // تحديث حالة الدفع
                        deal.IsChangePaymentProcessed = true;
                        deal.ChangePaymentProcessedAt = DateTime.UtcNow;

                        await _unitOfWork.Deal.UpdateAsync(deal);
                        await _unitOfWork.Complete();

                        // إرسال إشعارات
                        await SendChangeCompletedNotifications(deal, true, Math.Abs(deal.ChangeAmountDifference ?? 0));

                        return Ok(new
                        {
                            Message = "Change payment confirmed and changes applied successfully.",
                            NewContractUrl = deal.ContractDocumentUrl,
                            ContractVersion = deal.ContractVersion
                        });
                    }
                    else
                    {
                        return BadRequest(new { Message = "Payment was not successful." });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error confirming change payment for deal {request.DealId}");
                    return StatusCode(500, new { Message = "Error confirming change payment", Error = ex.Message });
                }
            }

        [HttpGet("change-payment-success")]
        public async Task<IActionResult> ChangePaymentSuccess([FromQuery] string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
                return BadRequest(new { Message = "Session ID is required." });

            try
            {
                StripeConfiguration.ApiKey = _stripeSecretKey;
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                var sessionService = new SessionService(stripeClient);
                var session = await sessionService.GetAsync(session_id);

                if (session.PaymentStatus != "paid")
                    return BadRequest(new { Message = "Payment has not been completed." });

                if (!session.Metadata.TryGetValue("DealId", out var dealIdStr) ||
                    !int.TryParse(dealIdStr, out var dealId) ||
                    !session.Metadata.TryGetValue("ChangeRequestId", out var changeRequestIdStr) ||
                    !int.TryParse(changeRequestIdStr, out var changeRequestId))
                {
                    return BadRequest(new { Message = "Invalid session metadata." });
                }

                var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
                var changeRequest = await _unitOfWork.DealChangeRequest.GetWithDetailsAsync(changeRequestId);

                if (deal == null || changeRequest == null)
                    return NotFound(new { Message = "Deal or change request not found." });

                // تطبيق التغييرات
                await ApplyChangesToDeal(deal, changeRequest);

                // إنشاء عقد جديد
                await GenerateChangeContract(deal);

                // تحديث الدفع
                deal.IsChangePaymentProcessed = true;
                deal.ChangePaymentProcessedAt = DateTime.UtcNow;
                deal.ChangePaymentIntentId = session.PaymentIntentId;

                await _unitOfWork.Deal.UpdateAsync(deal);
                await _unitOfWork.Complete();

                // إرسال إشعارات
                await SendChangeCompletedNotifications(deal, true, Math.Abs(deal.ChangeAmountDifference ?? 0));

                return Redirect($"{_configuration["ClientBaseUrl"]}/deal/change-success?dealId={deal.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing change payment success");
                return StatusCode(500, new { Message = "Error processing payment success" });
            }
        }

        [HttpPost("process-profit-payment")]
        public async Task<IActionResult> ProcessProfitPayment(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ProcessProfitPaymentDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Ensure user is admin
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);
            if (!isAdmin)
                return Unauthorized(new { Message = "Only admins can process profit payments." });

            var profitDistribution = await _unitOfWork.InvestmentProfit.GetByIdAsync(request.ProfitDistributionId);
            if (profitDistribution == null)
                return NotFound(new { Message = "Profit distribution not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(profitDistribution.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            if (deal.Status != DealStatus.Active)
                return BadRequest(new { Message = "Deal is not active" });

            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // Calculate period (typically previous month or specified period)
                //string period = request.Period ?? DateTime.UtcNow.AddMonths(-1).ToString("MMM yyyy");

                // Calculate profits based on sales data
                //var profitDistribution = await CalculateProfitFromSalesData(deal, period);

                // Admin approval is automatic since this is done by an admin
                //profitDistribution.IsApprovedByAdmin = true;
                //profitDistribution.AdminId = userId;
                //profitDistribution.ApprovalDate = DateTime.UtcNow;
                //profitDistribution.IsPending = false;

                // Save the profit distribution record
                //await _unitOfWork.InvestmentProfit.AddAsync(profitDistribution);
                //await _unitOfWork.Complete();

                // Process the payment through Stripe
                var transfer = await DistributeProfits(profitDistribution);

                // Mark as paid
                profitDistribution.IsPaid = true;
                await _unitOfWork.InvestmentProfit.UpdateAsync(profitDistribution);

                // Record the transaction
                var transaction = new DealTransaction
                {
                    DealId = deal.Id,
                    Amount = profitDistribution.InvestorShare,
                    Type = TransactionType.ProfitDistributionToInvestor,
                    TransactionId = transfer.Id,
                    Description = $"Profit distribution to investor for period {profitDistribution.StartDate.ToString("yyyy-MM-dd")} to {profitDistribution.EndDate.ToString("yyyy-MM-dd")} (calculated from sales data)"
                };

                await _unitOfWork.InvestmentTransaction.AddAsync(transaction);

                // Notify the investor
                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId, // استخدام معرف المسؤول
                    RecipientId = deal.InvestorId!,
                    MessageText = $"Your profit share of {profitDistribution.InvestorShare:C} for period from {profitDistribution.StartDate.ToString("yyyy-MM-dd")} to {profitDistribution.EndDate.ToString("yyyy-MM-dd")} " +
                                  $"has been calculated from actual sales data and processed. Total product sales: {profitDistribution.TotalRevenue / profitDistribution.ManufacturingCost:F0} units, " +
                                  $"Revenue: {profitDistribution.TotalRevenue:C}.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                // Notify the business owner
                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId, // استخدام معرف المسؤول
                    RecipientId = deal.AuthorId,
                    MessageText = $"Profit distribution of {profitDistribution.NetProfit:C} for period {profitDistribution.StartDate.ToString("yyyy-MM-dd")} to {profitDistribution.EndDate.ToString("yyyy-MM-dd")} " +
                                  $"has been processed based on your sales data. Your share: {profitDistribution.OwnerShare:C}. " +
                                  $"Sales: {profitDistribution.TotalRevenue / profitDistribution.ManufacturingCost:F0} units, Revenue: {profitDistribution.TotalRevenue:C}.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Profit payment processed successfully based on sales data.",
                    TransferId = transfer.Id,
                    Details = new
                    {
                        TotalRevenue = profitDistribution.TotalRevenue,
                        ManufacturingCost = profitDistribution.ManufacturingCost,
                        OtherCosts = profitDistribution.OtherCosts,
                        NetProfit = profitDistribution.NetProfit,
                        InvestorShare = profitDistribution.InvestorShare,
                        OwnerShare = profitDistribution.OwnerShare,
                        PlatformFee = profitDistribution.PlatformFee,
                        UnitsSold = profitDistribution.TotalRevenue / profitDistribution.ManufacturingCost
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing profit payment for profit distribution {request.ProfitDistributionId}");
                return StatusCode(500, new { Message = "Error processing profit payment", Error = ex.Message });
            }
        }

        [HttpPost("connect-stripe-account")]
        public async Task<IActionResult> ConnectStripeAccount(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ConnectStripeAccountDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
            if (currentUser == null)
                return Unauthorized(new { Message = "Only business owners can connect Stripe accounts." });

            var user = await _unitOfWork.Auth.GetUserById(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });


            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                string platform = request.Platform ?? "web"; // Default to web if not specified

                // Create a connected account
                var account = await CreateConnectedAccount(user, platform);

                // Save the account ID to the user
                user.StripeAccountId = account.Id;
                await _unitOfWork.Auth.UpdateUser(user);

                // Create URLs based on platform
                string refreshUrl, returnUrl;

                if (platform == "mobile")
                {
                    // استخدم روابط صالحة كاملة للموبايل
                    refreshUrl = "https://innova-hub.premiumasp.net/api/payment/stripe-refresh";
                    returnUrl = "https://innova-hub.premiumasp.net/api/payment/stripe-return";
                }
                else // web
                {
                    // استخدم روابط صالحة كاملة للويب
                    refreshUrl = "https://innova-hub.premiumasp.net/api/payment/stripe-refresh";
                    returnUrl = "https://innova-hub.premiumasp.net/api/payment/stripe-return";
                }

                // Create an account link for onboarding
                var link = await CreateAccountLink(
                    account.Id, refreshUrl, returnUrl, platform);

                return Ok(new
                {
                    Message = "Stripe account created successfully.",
                    AccountId = account.Id,
                    OnboardingUrl = link.Url
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe connected account for user {UserId}", userId);

                return StatusCode(500, new { Message = "Error creating Stripe account", Error = ex.Message });
            }
        }

        [HttpGet("stripe-return")]
        public async Task<IActionResult> StripeReturn([FromQuery] string account_id)
        {
            if (string.IsNullOrEmpty(account_id))
            {
                return BadRequest(new { Message = "Account ID is required." });
            }

            try
            {
                // تحديث حالة الحساب في قاعدة البيانات
                var user = await _unitOfWork.Auth.GetUserByStripeAccountId(account_id);
                if (user != null)
                {
                    // استخدام IsStripeAccountEnabled لتمييز اكتمال الإعداد
                    user.IsStripeAccountEnabled = true;

                    // التحديث في قاعدة البيانات
                    await _unitOfWork.Auth.UpdateUser(user);
                    await _unitOfWork.Complete();

                    _logger.LogInformation($"تم تحديث حالة حساب Stripe للمستخدم {user.Id} إلى 'ممكّن'");
                }

                // إعادة توجيه المستخدم إلى صفحة الملف الشخصي
                return Redirect($"{_configuration["ClientBaseUrl"]}/profile?stripeConnected=true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء معالجة العودة من Stripe Connect للحساب {AccountId}", account_id);
                return StatusCode(500, new { Message = "حدث خطأ أثناء معالجة طلبك." });
            }
        }

        [HttpGet("stripe-refresh")]
        public IActionResult StripeRefresh()
        {
            // إعادة توجيه المستخدم إلى صفحة الملف الشخصي
            return Redirect($"{_configuration["ClientBaseUrl"]}/profile?refreshStripe=true");
        }

        [HttpGet("stripe-account-status")]
        public async Task<IActionResult> GetStripeAccountStatus(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var user = await _unitOfWork.Auth.GetUserById(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            if (string.IsNullOrEmpty(user.StripeAccountId))
                return Ok(new { IsConnected = false });

            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                var account = await GetConnectedAccount(user.StripeAccountId);

                return Ok(new
                {
                    IsConnected = true,
                    AccountId = account.Id,
                    ChargesEnabled = account.ChargesEnabled,
                    PayoutsEnabled = account.PayoutsEnabled,
                    DetailsSubmitted = account.DetailsSubmitted,
                    Requirements = new
                    {
                        CurrentlyDue = account.Requirements?.CurrentlyDue,
                        EventuallyDue = account.Requirements?.EventuallyDue,
                        PastDue = account.Requirements?.PastDue
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Stripe account for user {UserId}", userId);

                return StatusCode(500, new { Message = "Error retrieving Stripe account", Error = ex.Message });
            }
        }

        [HttpGet("payment-success")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
                return BadRequest(new { Message = "Session ID is required." });

            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // إنشاء عميل Stripe صريح
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                // استخدام العميل الصريح مع خدمة Session
                var sessionService = new SessionService(stripeClient);
                var session = await sessionService.GetAsync(session_id);

                if (session.PaymentStatus != "paid")
                    return BadRequest(new { Message = "Payment has not been completed." });

                // استخراج معرف الاستثمار من الميتاداتا
                if (!session.Metadata.TryGetValue("DealId", out var dealIdStr) ||
                    !int.TryParse(dealIdStr, out var dealId))
                {
                    _logger.LogError("لا يمكن استخراج معرف الاستثمار من الميتاداتا");
                    return BadRequest(new { Message = "Invalid session metadata." });
                }

                _logger.LogInformation($"تم استخراج معرف الاستثمار: {dealId}");
                var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
                if (deal == null)
                {
                    _logger.LogError($"الاستثمار رقم {dealId} غير موجود");
                    return NotFound(new { Message = "Deal not found." });
                }

                // سجل القيم للتشخيص
                _logger.LogInformation($"Session PaymentIntentId: {session.PaymentIntentId}");
                _logger.LogInformation($"Deal PaymentIntentId: {deal.PaymentIntentId}");

                // تحديث معرف الدفع في الاستثمار بدلاً من التحقق
                deal.PaymentIntentId = session.PaymentIntentId;
                _logger.LogInformation($"تم تحديث PaymentIntentId للاستثمار: {session.PaymentIntentId}");

                // تحديث حالة دفع الاستثمار
                deal.IsPaymentProcessed = true;
                deal.PaymentProcessedAt = DateTime.UtcNow;
                deal.PaymentStatus = "completed";
                deal.Status = DealStatus.Active;
                _logger.LogInformation("تم تحديث حالة الاستثمار إلى 'active'");

                // Create a new product from this deal
                var product = new InnoHub.Core.Models.Product
                {
                    Name = deal.BusinessName,
                    Description = deal.Description,
                    HomePicture = deal.Pictures.FirstOrDefault() ?? "",
                    AuthorId = deal.AuthorId!,
                    CategoryId = deal.CategoryId,
                    Price = deal.EstimatedPrice,
                    ProductPictures = ConvertDealPicturesToProductPictures(deal.Pictures)
                };

                // Add the product to the database
                await _unitOfWork.Product.AddAsync(product);

                // Update the deal with the product reference
                deal.ProductId = product.Id;
                deal.IsProductCreated = true;

                await _unitOfWork.Deal.UpdateAsync(deal);
                await _unitOfWork.Complete();

                // Generate contract AFTER payment confirmation
                try
                {
                    await GenerateContractDocument(deal);

                    // Notify parties about contract ready for signing
                    await NotifyPartiesAboutContractGeneration(deal);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error generating contract for deal {dealId}");
                }

                await _unitOfWork.Deal.UpdateAsync(deal);
                await _unitOfWork.Complete();

                // إنشاء الإشعارات
                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                    RecipientId = deal.AuthorId,
                    MessageText = $"The payment for the deal in '{deal.BusinessName}' has been successfully processed. " +
                                  $"Amount: {deal.OfferMoney:C}. Please confirm to complete the deal.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                    RecipientId = deal.InvestorId!,
                    MessageText = $"Your payment for the deal in '{deal.BusinessName}' has been successfully processed. " +
                                  $"Amount: {deal.OfferMoney:C}. Awaiting the business owner's confirmation.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                _logger.LogInformation("تم إنشاء الإشعارات");

                await _unitOfWork.Complete();
                _logger.LogInformation("تم حفظ التغييرات في قاعدة البيانات");

                // إعادة توجيه إلى صفحة النجاح
                return Redirect($"{_configuration["ClientBaseUrl"]}/order/payment-success?dealId={deal.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "حدث خطأ أثناء معالجة نجاح الدفع للجلسة {SessionId}", session_id);
                return StatusCode(500, new { Message = "An error occurred while processing the payment.", Error = ex.Message });
            }
        }

        [HttpGet("payment-cancel")]
        public IActionResult PaymentCancel()
        {
            // Return direct response instead of redirecting
            return Ok(new
            {
                Message = "The payment process was successfully canceled.",
                Status = "cancelled"
            });
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeWebhookSecret
                );

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(stripeEvent.Data.Object as PaymentIntent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailed(stripeEvent.Data.Object as PaymentIntent);
                        break;

                    case "transfer.created":
                        await HandleTransferCreated(stripeEvent.Data.Object as Transfer);
                        break;

                    case "account.updated":
                        await HandleAccountUpdated(stripeEvent.Data.Object as Account);
                        break;

                    case "charge.refunded":
                        await HandleChargeRefunded(stripeEvent.Data.Object as Charge);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return BadRequest();
            }
        }
        #endregion

        #region Helper Methods

        // Web-specific checkout session creation
        private async Task<Session> CreateDealCheckoutSession(Deal deal, string successUrl, string cancelUrl)
        {
            try
            {
                // تأكد من أنك تستخدم SecretKey
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey); // استخدم الـ Secret Key هنا

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(deal.OfferMoney * 100), // Convert to cents
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Deal in {deal.BusinessName}",
                                    Description = $"{deal.OfferDeal}% equity stake in {deal.BusinessName}"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = cancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "DealId", deal.Id.ToString() },
                        { "InvestorId", deal.InvestorId! },
                        { "PaymentType", "Investment" },
                        { "Platform", "web" }
                    }
                };

                var service = new SessionService(stripeClient); // استخدم الـ stripeClient مع الـ Secret Key
                var session = await service.CreateAsync(options);

                // Save the payment intent ID to the deal
                if (session.PaymentIntentId != null)
                {
                    deal.PaymentIntentId = session.PaymentIntentId;
                }
                deal.Platform = "web";

                return session;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error creating Stripe payment session for deal {deal.Id}");
                throw new ApplicationException($"Error processing payment: {ex.Message}", ex);
            }
        }

        // Mobile-specific payment intent creation
        private async Task<(string clientSecret, string paymentIntentId)> CreateDealPaymentIntent(Deal deal)
        {
            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // إنشاء عميل Stripe صريح
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(deal.OfferMoney * 100), // Convert to cents
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = $"Investment in {deal.BusinessName} - {deal.OfferDeal}% equity",
                    Metadata = new Dictionary<string, string>
                    {
                        { "DealId", deal.Id.ToString() },
                        { "InvestorId", deal.InvestorId! },
                        { "PaymentType", "Investment" },
                        { "Platform", "mobile" }
                    },
                    // Enable these capabilities for Apple Pay / Google Pay support
                    PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
                    {
                        Card = new PaymentIntentPaymentMethodOptionsCardOptions
                        {
                            RequestThreeDSecure = "automatic"
                        }
                    }
                };

                var service = new PaymentIntentService(stripeClient);
                var intent = await service.CreateAsync(options);

                // Save the payment intent details to the deal
                deal.PaymentIntentId = intent.Id;
                deal.PaymentClientSecret = intent.ClientSecret;
                deal.Platform = "mobile";

                return (intent.ClientSecret, intent.Id);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error creating Stripe payment intent for deal {deal.Id}");
                throw new ApplicationException($"Error processing payment: {ex.Message}", ex);
            }
        }

        private async Task<Transfer> DistributeProfits(DealProfit profitDistribution)
        {
            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // إنشاء عميل Stripe صريح
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                var deal = profitDistribution.Deal;

                if (string.IsNullOrEmpty(deal.StripeAccountId))
                {
                    throw new ApplicationException("Business owner does not have a connected Stripe account");
                }

                // Create a payment intent for the profit distribution
                var paymentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(profitDistribution.InvestorShare * 100), // Convert to cents
                    Currency = "usd",
                    PaymentMethod = "pm_card_visa", // This would be replaced with actual payment method in production
                    Confirm = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "DealId", deal.Id.ToString() },
                        { "ProfitDistributionId", profitDistribution.Id.ToString() },
                        { "PaymentType", "ProfitDistribution" }
                    }
                };

                var paymentService = new PaymentIntentService(stripeClient);
                var paymentIntent = await paymentService.CreateAsync(paymentOptions);

                // Transfer funds to the investor
                var transferOptions = new TransferCreateOptions
                {
                    Amount = (long)(profitDistribution.InvestorShare * 100), // Convert to cents
                    Currency = "usd",
                    Destination = deal.StripeAccountId, // In a real scenario, the investor would have their own connected account
                    SourceTransaction = paymentIntent.Id,
                    Metadata = new Dictionary<string, string>
                    {
                        { "DealId", deal.Id.ToString() },
                        { "ProfitDistributionId", profitDistribution.Id.ToString() },
                        { "TransferType", "ProfitDistribution" }
                    }
                };

                var transferService = new TransferService(stripeClient);
                var transfer = await transferService.CreateAsync(transferOptions);

                // Mark the profit distribution as paid
                profitDistribution.IsPaid = true;

                return transfer;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error distributing profits for deal {profitDistribution.DealId}");
                throw new ApplicationException($"Error distributing profits: {ex.Message}", ex);
            }
        }

        private async Task<Account> CreateConnectedAccount(AppUser user, string platform = "web")
        {
            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // إنشاء عميل Stripe صريح
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                var options = new AccountCreateOptions
                {
                    Type = "express",
                    Country = "US",  // أو الدولة التي تختارها
                    Email = user.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        Transfers = new AccountCapabilitiesTransfersOptions
                        {
                            Requested = true
                        },
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions
                        {
                            Requested = true
                        }
                    },
                    BusinessType = "individual",
                    BusinessProfile = new AccountBusinessProfileOptions
                    {
                        Name = $"{user.FirstName} {user.LastName}'s Business",
                        Url = "https://innova-hub.premiumasp.net"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", user.Id },
                        { "Platform", platform }
                    }
                };

                var service = new AccountService(stripeClient);
                var account = await service.CreateAsync(options);

                return account;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error creating Stripe connected account for user {user.Id}");
                throw new ApplicationException($"Error creating Stripe account: {ex.Message}", ex);
            }
        }

        private async Task<Account> GetConnectedAccount(string accountId)
        {
            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // إنشاء عميل Stripe صريح
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                var service = new AccountService(stripeClient);
                var account = await service.GetAsync(accountId);

                return account;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error retrieving Stripe connected account {accountId}");
                throw new ApplicationException($"Error retrieving Stripe account: {ex.Message}", ex);
            }
        }

        private async Task<AccountLink> CreateAccountLink(string accountId, string refreshUrl, string returnUrl, string platform)
        {
            try
            {
                // تأكد من تعيين مفتاح API
                StripeConfiguration.ApiKey = _stripeSecretKey;

                // إنشاء عميل Stripe صريح
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);

                string finalRefreshUrl = EnsureValidUrl(refreshUrl);
                string finalReturnUrl = EnsureValidUrl(returnUrl);

                var options = new AccountLinkCreateOptions
                {
                    Account = accountId,
                    RefreshUrl = finalRefreshUrl,
                    ReturnUrl = finalReturnUrl,
                    Type = "account_onboarding"
                };

                var service = new AccountLinkService();
                return await service.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error creating account link for Stripe account {accountId}");
                throw new ApplicationException($"Error creating account link: {ex.Message}", ex);
            }
        }

        private string EnsureValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                // استخدم رابط افتراضي إذا كان الرابط فارغًا
                return "https://innova-hub.premiumasp.net/stripe-return";
            }

            // تأكد من أن الرابط يبدأ بـ http:// أو https://
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                return "https://" + url;
            }

            return url;
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent paymentIntent)
        {
            // تأكد من تعيين مفتاح API
            StripeConfiguration.ApiKey = _stripeSecretKey;

            if (paymentIntent.Metadata.TryGetValue("DealId", out var dealIdStr) &&
                int.TryParse(dealIdStr, out var dealId))
            {
                var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
                if (deal != null)
                {
                    
                }
            }
            else if (paymentIntent.Metadata.TryGetValue("ProfitDistributionId", out var profitIdStr) &&
                    int.TryParse(profitIdStr, out var profitId))
            {
                var profitDistribution = await _unitOfWork.InvestmentProfit.GetByIdAsync(profitId);
                if (profitDistribution != null)
                {
                    profitDistribution.IsPaid = true;

                    await _unitOfWork.InvestmentProfit.UpdateAsync(profitDistribution);
                    await _unitOfWork.Complete();

                    _logger.LogInformation($"Profit distribution payment succeeded for profit distribution {profitId}");
                }
            }
        }

        private async Task HandlePaymentIntentFailed(PaymentIntent paymentIntent)
        {
            // تأكد من تعيين مفتاح API
            StripeConfiguration.ApiKey = _stripeSecretKey;

            if (paymentIntent.Metadata.TryGetValue("DealId", out var dealIdStr) &&
                int.TryParse(dealIdStr, out var dealId))
            {
                var deal = await _unitOfWork.Deal.GetByIdAsync(dealId);
                if (deal != null)
                {
                    deal.PaymentStatus = "failed";
                    deal.PaymentError = paymentIntent.LastPaymentError?.Message;

                    await _unitOfWork.Deal.UpdateAsync(deal);

                    // Log payment failure
                    var failureLog = new PaymentFailureLog
                    {
                        UserId = deal.InvestorId!,
                        UserEmail = (await _unitOfWork.Auth.GetUserById(deal.InvestorId!))?.Email!,
                        PaymentIntentId = paymentIntent.Id,
                        FailureReason = paymentIntent.LastPaymentError?.Message,
                        FailedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.PaymentFailureLog.AddAsync(failureLog);
                    await _unitOfWork.Complete();

                    _logger.LogWarning($"Payment failed for deal {deal.Id}",
                        dealId, deal.PaymentError);
                }
            }
        }

        private async Task HandleTransferCreated(Transfer transfer)
        {
            try
            {
                // تحقق مما إذا كان التحويل مرتبط باستثمار أو توزيع أرباح
                if (transfer.Metadata.TryGetValue("DealId", out var dealIdStr) &&
                    int.TryParse(dealIdStr, out var dealId))
                {
                    _logger.LogInformation("تم إنشاء تحويل للاستثمار {DealId}: المبلغ {Amount}",
                        dealId, transfer.Amount / 100.0m);

                    // حفظ سجل التحويل في قاعدة البيانات
                    var transaction = new DealTransaction
                    {
                        DealId = dealId,
                        Amount = transfer.Amount / 100.0m,
                        Type = TransactionType.ProfitDistributionToOwner,
                        TransactionId = transfer.Id,
                        Description = $"Transferring profits to the business owner."
                        // CreatedAt تم حذفها لأنها غير موجودة في النموذج
                    };

                    await _unitOfWork.InvestmentTransaction.AddAsync(transaction);

                    // إذا كان هذا التحويل مرتبط بتوزيع أرباح محدد
                    if (transfer.Metadata.TryGetValue("ProfitDistributionId", out var profitIdStr) &&
                        int.TryParse(profitIdStr, out var profitId))
                    {
                        var profitDistribution = await _unitOfWork.InvestmentProfit.GetByIdAsync(profitId);
                        if (profitDistribution != null)
                        {
                            profitDistribution.IsPaid = true;
                            // تم حذف PaidAt لأنها غير موجودة
                            await _unitOfWork.InvestmentProfit.UpdateAsync(profitDistribution);
                        }
                    }

                    // تم حذف تحديث LastProfitTransferAt لأنها غير موجودة في نموذج Investment

                    await _unitOfWork.Complete();
                }
                else if (transfer.Metadata.TryGetValue("RefundId", out var refundIdStr))
                {
                    // معالجة تحويلات الاسترداد إذا لزم الأمر
                    _logger.LogInformation("تم إنشاء تحويل استرداد: {RefundId}, المبلغ {Amount}",
                        refundIdStr, transfer.Amount / 100.0m);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ أثناء معالجة تحويل Stripe: {transfer.Id}");
            }
        }

        private async Task HandleAccountUpdated(Account account)
        {
            try
            {
                // البحث عن المستخدم المرتبط بهذا الحساب في Stripe
                var user = await _unitOfWork.Auth.GetUserByStripeAccountId(account.Id);
                if (user != null)
                {
                    _logger.LogInformation("تم تحديث حساب Stripe للمستخدم {UserId}: تمكين المدفوعات: {ChargesEnabled}, تمكين التحويلات: {PayoutsEnabled}",
                        user.Id, account.ChargesEnabled, account.PayoutsEnabled);

                    // تحديث حالة حساب Stripe للمستخدم
                    user.IsStripeAccountEnabled = account.PayoutsEnabled && account.ChargesEnabled;

                    // تحديث المستخدم في قاعدة البيانات
                    await _unitOfWork.Auth.UpdateUser(user);
                    await _unitOfWork.Complete();

                    // إرسال إشعار للمستخدم إذا اكتمل الإعداد (اختياري)
                    if (account.DetailsSubmitted && account.PayoutsEnabled && !user.IsStripeAccountEnabled)
                    {
                        var notification = new DealMessage
                        {
                            SenderId = _adminUserId,
                            RecipientId = user.Id,
                            MessageText = "Your Stripe account has been successfully activated! You can now receive payments and profits.",
                            IsRead = false,
                            MessageType = MessageType.General,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.InvestmentMessage.AddAsync(notification);
                        await _unitOfWork.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ أثناء معالجة تحديث حساب Stripe: {account.Id}");
            }
        }

        private async Task HandleChargeRefunded(Charge charge)
        {
            if (charge.PaymentIntent != null &&
                await _unitOfWork.Deal.GetByPaymentIntentId(charge.PaymentIntentId) is var deal &&
                deal != null)
            {
                _logger.LogInformation("Refund processed for deal {InvestmentId}: {Amount}",
                    deal.Id, charge.AmountRefunded / 100.0m);

                // Create a refund record
                var refundLog = new PaymentRefundLog
                {
                    OrderId = deal.Id,
                    RefundAmount = charge.AmountRefunded / 100.0m,
                    RefundId = charge.Id,
                    RefundStatus = charge.Status,
                    RefundCreated = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRefundLog.AddAsync(refundLog);
                await _unitOfWork.Complete();
            }
        }

        //private async Task<DealProfit> CalculateProfitFromSalesData(Deal deal, string period)
        //{
        //    if (deal.ProductId == null)
        //        throw new InvalidOperationException("Deal must be linked to a product for automatic profit calculation.");

        //    // Parse period dates
        //    string[] periodParts = period.Split(' ');
        //    if (periodParts.Length != 2)
        //        throw new ArgumentException("Invalid period format. Use 'MMM yyyy' format (e.g., 'Jan 2025')");

        //    string monthName = periodParts[0];
        //    int year = int.Parse(periodParts[1]);
        //    DateTime startDate = new DateTime(year, DateTime.ParseExact(monthName, "MMM", CultureInfo.InvariantCulture).Month, 1);
        //    DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        //    // Get sales data for the period
        //    var orderItems = await _unitOfWork.OrderItem.GetByProductIdAndDateRange(
        //        deal.ProductId.Value, startDate, endDate);

        //    // Calculate profit from sales
        //    decimal totalRevenue = orderItems.Sum(item => item.Price * item.Quantity);
        //    int totalQuantitySold = orderItems.Sum(item => item.Quantity);
        //    decimal manufacturingCost = deal.ManufacturingCost * totalQuantitySold;
        //    decimal otherCosts = totalRevenue * 0.15m; // Operating costs as percentage of revenue

        //    decimal netProfit = totalRevenue - manufacturingCost - otherCosts;

        //    if (netProfit <= 0)
        //        throw new InvalidOperationException("No profit to distribute for this period.");

        //    // Calculate shares based on the deal agreement
        //    decimal investorShare = netProfit * (deal.OfferDeal / 100);
        //    decimal platformFee = investorShare * (deal.PlatformFeePercentage / 100);
        //    decimal ownerShare = netProfit - investorShare;

        //    // Create profit distribution record
        //    return new DealProfit
        //    {
        //        DealId = deal.Id,
        //        TotalRevenue = totalRevenue,
        //        ManufacturingCost = manufacturingCost,
        //        OtherCosts = otherCosts,
        //        NetProfit = netProfit,
        //        InvestorShare = investorShare - platformFee,
        //        OwnerShare = ownerShare,
        //        PlatformFee = platformFee,
        //        DistributionDate = DateTime.UtcNow,
        //        Period = period,
        //        IsPaid = false
        //    };
        //}

        private async Task GenerateContractDocument(Deal deal)
        {
            // Create a directory for contracts
            var directoryPath = Path.Combine(_environment.WebRootPath, "Contracts");
            _unitOfWork.FileService.EnsureDirectory(directoryPath);

            // Generate contract filename
            var fileName = $"contract_deal_{deal.Id}_{DateTime.UtcNow.Ticks}.pdf";
            var filePath = Path.Combine(directoryPath, fileName);

            // Get owner and investor details
            var owner = await _unitOfWork.Auth.GetUserById(deal.AuthorId);
            var investor = await _unitOfWork.Auth.GetUserById(deal.InvestorId!);

            // Check if signatures are available
            bool hasOwnerSignature = !string.IsNullOrEmpty(owner?.SignatureImageUrl);
            bool hasInvestorSignature = !string.IsNullOrEmpty(investor?.SignatureImageUrl);

            // Get signature image paths if available
            string ownerSignaturePath = hasOwnerSignature
                ? _unitOfWork.FileService.GetAbsolutePath(owner.SignatureImageUrl.TrimStart('/'))
                : null;

            string investorSignaturePath = hasInvestorSignature
                ? _unitOfWork.FileService.GetAbsolutePath(investor.SignatureImageUrl.TrimStart('/'))
                : null;

            // Generate PDF contract
            using (var document = new iTextSharp.text.Document())
            {
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                // Add contract header
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                document.Add(new Paragraph("INVESTMENT AGREEMENT", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph($"Contract ID: {deal.Id}", smallFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Date: {DateTime.UtcNow:MMMM dd, yyyy}", smallFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(Chunk.NEWLINE);

                // Add parties
                document.Add(new Paragraph("THIS INVESTMENT AGREEMENT (\"Agreement\") is made and entered into on the date signed below, by and between:", normalFont));
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph($"{owner!.FirstName} {owner.LastName}, hereinafter referred to as the \"Business Owner\"", normalFont));
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph("AND", normalFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph($"{investor!.FirstName} {investor.LastName}, hereinafter referred to as the \"Investor\"", normalFont));
                document.Add(Chunk.NEWLINE);

                // Add deal details
                document.Add(new Paragraph("WHEREAS:", normalFont));
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph($"1. The Business Owner owns and operates a business known as \"{deal.BusinessName}\".", normalFont));
                document.Add(new Paragraph($"2. The Investor wishes to invest in the Business Owner's enterprise.", normalFont));
                document.Add(new Paragraph("3. Both parties agree to enter into this Agreement under the following terms and conditions:", normalFont));
                document.Add(Chunk.NEWLINE);

                // Add investment details
                document.Add(new Paragraph("TERMS OF AGREEMENT:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph($"1. INVESTMENT AMOUNT: The Investor agrees to invest {deal.OfferMoney:C} in the Business Owner's enterprise.", normalFont));

                document.Add(new Paragraph($"2. EQUITY PERCENTAGE: In exchange for the investment, the Investor will receive {deal.OfferDeal}% of the net profits generated by the enterprise, in accordance with Islamic Mudarabah principles.", normalFont));

                document.Add(new Paragraph($"3. INVESTMENT DURATION: This investment shall be for a period of {deal.DurationInMonths} months from the date of full execution of this Agreement.", normalFont));

                document.Add(new Paragraph($"5. PROFIT DISTRIBUTION: Profits shall be calculated after deducting all operational costs, including but not limited to manufacturing costs of {deal.ManufacturingCost:C} per unit and all other legitimate business expenses.", normalFont));

                document.Add(new Paragraph("6. LOSS BEARING: In accordance with Islamic Mudarabah principles, any financial losses shall be borne by the Investor (Rab al-Mal), while the Business Owner (Mudarib) loses only their time and effort, provided that there has been no negligence or misconduct on the part of the Business Owner.", normalFont));

                document.Add(new Paragraph("7. PLATFORM FEE: A fee of 1% of the Investor's profit will be deducted and paid to InnoHub as a platform facilitation fee.", normalFont));

                document.Add(new Paragraph("8. CONVERSION TO PRODUCT: The Business Owner may create products based on this investment, while maintaining the agreed profit sharing structure.", normalFont));

                document.Add(new Paragraph("9. TERMINATION: Either party may request early termination of this investment. Mutual agreement is required unless breach of terms or fraud is demonstrated.", normalFont));

                // Add signature placeholders
                document.Add(new Paragraph("IN WITNESS WHEREOF, the parties have executed this Agreement as of the date first above written.", normalFont));
                document.Add(Chunk.NEWLINE);
                document.Add(Chunk.NEWLINE);

                // Create a table for signatures
                var table = new PdfPTable(2);
                table.WidthPercentage = 100;

                // Business Owner signature
                var ownerCell = new PdfPCell();
                ownerCell.AddElement(new Paragraph("Business Owner Signature:", normalFont));

                // Add owner signature image if available
                if (hasOwnerSignature && System.IO.File.Exists(ownerSignaturePath))
                {
                    try
                    {
                        iTextSharp.text.Image ownerSignature = iTextSharp.text.Image.GetInstance(ownerSignaturePath);
                        ownerSignature.ScaleToFit(150, 75);
                        ownerCell.AddElement(ownerSignature);

                        // Update deal status to reflect owner has signed
                        deal.IsOwnerSigned = true;
                        deal.OwnerSignedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with the method
                        // If signature can't be embedded, fall back to placeholder
                        ownerCell.AddElement(new Paragraph("____________________", normalFont));
                        _logger.LogError(ex, $"Error embedding owner signature for deal {deal.Id}");
                    }
                }
                else
                {
                    ownerCell.AddElement(new Paragraph("____________________", normalFont));
                }

                ownerCell.AddElement(new Paragraph($"Name: {owner?.FirstName} {owner?.LastName}", normalFont));
                ownerCell.AddElement(new Paragraph($"Date: {(deal.IsOwnerSigned ? DateTime.UtcNow.ToString("MM/dd/yyyy") : "________________")}", normalFont));
                ownerCell.Border = PdfPCell.NO_BORDER;
                table.AddCell(ownerCell);

                // Investor signature
                var investorCell = new PdfPCell();
                investorCell.AddElement(new Paragraph("Investor Signature:", normalFont));

                // Add investor signature image if available
                if (hasInvestorSignature && System.IO.File.Exists(investorSignaturePath))
                {
                    try
                    {
                        iTextSharp.text.Image investorSignature = iTextSharp.text.Image.GetInstance(investorSignaturePath);
                        investorSignature.ScaleToFit(150, 75);
                        investorCell.AddElement(investorSignature);

                        // Update deal status to reflect investor has signed
                        deal.IsInvestorSigned = true;
                        deal.InvestorSignedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with the method
                        // If signature can't be embedded, fall back to placeholder
                        investorCell.AddElement(new Paragraph("____________________", normalFont));
                        _logger.LogError(ex, $"Error embedding investor signature for deal {deal.Id}");
                    }
                }
                else
                {
                    investorCell.AddElement(new Paragraph("____________________", normalFont));
                }

                investorCell.AddElement(new Paragraph($"Name: {investor?.FirstName} {investor?.LastName}", normalFont));
                investorCell.AddElement(new Paragraph($"Date: {(deal.IsInvestorSigned ? DateTime.UtcNow.ToString("MM/dd/yyyy") : "________________")}", normalFont));
                investorCell.Border = PdfPCell.NO_BORDER;
                table.AddCell(investorCell);

                document.Add(table);

                // Calculate document hash for verification
                var documentHash = CalculateDocumentHash(deal, owner, investor);

                // Add embedded signature status to the document metadata
                document.Add(Chunk.NEWLINE);
                if (hasOwnerSignature || hasInvestorSignature)
                {
                    document.Add(new Paragraph("Embedded Signatures:", smallFont));
                    if (hasOwnerSignature && deal.IsOwnerSigned)
                        document.Add(new Paragraph("- Business Owner signature embedded on " + DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss"), smallFont));
                    if (hasInvestorSignature && deal.IsInvestorSigned)
                        document.Add(new Paragraph("- Investor signature embedded on " + DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss"), smallFont));
                }

                // Add verification footer
                document.Add(Chunk.NEWLINE);
                document.Add(new Paragraph($"Document Hash: {documentHash}", smallFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("This contract is electronically generated and is valid with signatures embedded or when countersigned by both parties through the InnoHub platform.", smallFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("If you cannot see embedded signatures above, please verify the agreement through the InnoHub platform.", smallFont) { Alignment = Element.ALIGN_CENTER });

                document.Close();
            }

            // Save the contract URL to the deal
            deal.ContractDocumentUrl = $"/Contracts/{fileName}";
            deal.ContractHash = CalculateDocumentHash(deal, owner, investor);

            // Check if both parties signed and update status accordingly
            if (deal.IsOwnerSigned && deal.IsInvestorSigned && deal.Status == DealStatus.AdminApproved)
            {
                deal.Status = DealStatus.Active;

                // Send notifications to both parties
                await SendContractSignedNotifications(deal);
            }
            else if (deal.IsOwnerSigned || deal.IsInvestorSigned)
            {
                // Notify the other party that one signature is already embedded
                await SendSignatureRequestNotification(deal, deal.IsOwnerSigned);
            }
        }

        private string CalculateDocumentHash(Deal deal, AppUser owner, AppUser investor)
        {
            // Create a string representation of key contract details
            var contractData = $"{deal.Id}|{owner.Id}|{investor.Id}|{deal.OfferMoney}|" +
                               $"{deal.OfferDeal}|{deal.DurationInMonths}|" +
                               $"{deal.ManufacturingCost}|{DateTime.UtcNow.Date}";

            // Calculate SHA-256 hash
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(contractData);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        private async Task NotifyPartiesAboutContractGeneration(Deal deal)
        {
            // Notify investor
            var investorNotification = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = deal.InvestorId!,
                MessageText = $"A contract has been generated for your deal in '{deal.BusinessName}'. Please review and sign the contract to proceed with the deal. " +
                              $"Contract URL: {deal.ContractDocumentUrl}",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow,
                ContractUrl = deal.ContractDocumentUrl
            };

            // Notify business owner
            var ownerNotification = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = deal.AuthorId,
                MessageText = $"A contract has been generated for your deal in '{deal.BusinessName}'. Please review and sign the contract to proceed with the deal. " +
                              $"Contract URL: {deal.ContractDocumentUrl}",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow,
                ContractUrl = deal.ContractDocumentUrl
            };

            await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
            await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
            await _unitOfWork.Complete();
        }

        private async Task SendSignatureRequestNotification(Deal deal, bool isOwnerSigned)
        {
            var recipientId = isOwnerSigned ? deal.InvestorId : deal.AuthorId;
            var senderName = isOwnerSigned ?
            $"{(await _unitOfWork.Auth.GetUserById(deal.AuthorId))?.FirstName} {(await _unitOfWork.Auth.GetUserById(deal.AuthorId))?.LastName}" :
            $"{(await _unitOfWork.Auth.GetUserById(deal.InvestorId!))?.FirstName} {(await _unitOfWork.Auth.GetUserById(deal.InvestorId!))?.LastName}";

            var message = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = recipientId!,
                MessageText = $"{senderName} has signed the deal contract for '{deal.BusinessName}'. Your signature is now required to finalize the agreement.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(message);
            await _unitOfWork.Complete();
        }

        private async Task SendContractSignedNotifications(Deal deal)
        {
            // Notify investor
            var investorMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = deal.InvestorId!,
                MessageText = $"The deal contract for '{deal.BusinessName}' has been fully executed. The deal is now active.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            // Notify business owner
            var ownerMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId, // استخدام معرف المسؤول بدلاً من "system"
                RecipientId = deal.AuthorId,
                MessageText = $"The deal contract for '{deal.BusinessName}' has been fully executed. The deal is now active.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
            await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
            await _unitOfWork.Complete();
        }

        // ========== New Helper Methods ==========

        private string GeneratePaymentHash(int dealId, string operation, decimal amount, string userId)
        {
            var data = $"{dealId}|{operation}|{amount}|{userId}|{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }

        private decimal? GetDecimalFromJson(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value is System.Text.Json.JsonElement element)
            {
                if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;
            }
            return null;
        }

            // ========== Change Payment Helper Methods ==========

            private async Task<Session> CreateChangeCheckoutSession(Deal deal, DealChangeRequest changeRequest, decimal amount, string changeType)
            {
                var sessionService = new SessionService();

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Deal Change - {deal.BusinessName}",
                            Description = changeType == "additional_investment"
                                ? "Additional investment required due to deal changes"
                                : "Deal modification payment"
                        }
                    },
                    Quantity = 1
                }
            },
                    Mode = "payment",
                    SuccessUrl = $"{_webBaseUrl}/api/Payment/change-payment-success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{_webBaseUrl}/api/Payment/payment-cancel",
                    Metadata = new Dictionary<string, string>
            {
                { "DealId", deal.Id.ToString() },
                { "ChangeRequestId", changeRequest.Id.ToString() },
                { "PaymentType", "Change" },
                { "ChangeType", changeType },
                { "Platform", "web" }
            }
                };

                return await sessionService.CreateAsync(options);
            }

            private async Task<(string clientSecret, string paymentIntentId)> CreateChangePaymentIntent(Deal deal, DealChangeRequest changeRequest, decimal amount, string changeType)
            {
                var stripeClient = new Stripe.StripeClient(_stripeSecretKey);
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = $"Deal Change - {deal.BusinessName}",
                    Metadata = new Dictionary<string, string>
            {
                { "DealId", deal.Id.ToString() },
                { "ChangeRequestId", changeRequest.Id.ToString() },
                { "PaymentType", "Change" },
                { "ChangeType", changeType },
                { "Platform", "mobile" }
            }
                };

                var service = new PaymentIntentService(stripeClient);
                var intent = await service.CreateAsync(options);
                return (intent.ClientSecret, intent.Id);
            }

            // ========== Contract Generation Methods ==========

            private async Task GenerateChangeContract(Deal deal)
            {
                // حفظ العقد السابق
                if (!string.IsNullOrEmpty(deal.ContractDocumentUrl))
                {
                    deal.PreviousContractDocumentUrl = deal.ContractDocumentUrl;
                }

                // زيادة رقم إصدار العقد
                deal.ContractVersion += 1;
                deal.LastContractGeneratedAt = DateTime.UtcNow;

                // إنشاء العقد الجديد
                await GenerateContractDocument(deal, "Amendment");
            }

            private async Task GenerateContractDocument(Deal deal, string contractType = "Original")
            {
                var directoryPath = Path.Combine(_environment.WebRootPath, "Contracts");
                _unitOfWork.FileService.EnsureDirectory(directoryPath);

                var fileName = $"contract_deal_{deal.Id}_v{deal.ContractVersion}_{contractType}_{DateTime.UtcNow.Ticks}.pdf";
                var filePath = Path.Combine(directoryPath, fileName);

                var owner = await _unitOfWork.Auth.GetUserById(deal.AuthorId);
                var investor = await _unitOfWork.Auth.GetUserById(deal.InvestorId!);

                using (var document = new iTextSharp.text.Document())
                {
                    PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                    document.Open();

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                    // عنوان العقد
                    string contractTitle = contractType switch
                    {
                        "Renewal" => "INVESTMENT AGREEMENT - RENEWAL",
                        "Amendment" => "INVESTMENT AGREEMENT - AMENDMENT",
                        _ => "INVESTMENT AGREEMENT"
                    };

                    document.Add(new Paragraph(contractTitle, titleFont) { Alignment = Element.ALIGN_CENTER });
                    document.Add(new Paragraph($"Contract ID: {deal.Id} (Version {deal.ContractVersion})", smallFont) { Alignment = Element.ALIGN_RIGHT });
                    document.Add(new Paragraph($"Contract Type: {contractType}", smallFont) { Alignment = Element.ALIGN_RIGHT });
                    document.Add(new Paragraph($"Date: {DateTime.UtcNow:MMMM dd, yyyy}", smallFont) { Alignment = Element.ALIGN_RIGHT });

                    if (deal.ContractVersion > 1)
                    {
                        document.Add(new Paragraph($"Previous Contract Version: {deal.ContractVersion - 1}", smallFont) { Alignment = Element.ALIGN_RIGHT });
                        document.Add(new Paragraph($"Supersedes all previous agreements", smallFont) { Alignment = Element.ALIGN_RIGHT });
                    }

                    document.Add(Chunk.NEWLINE);

                    // معلومات الأطراف
                    document.Add(new Paragraph($"THIS {contractType.ToUpper()} AGREEMENT is made between:", normalFont));
                    document.Add(Chunk.NEWLINE);

                    document.Add(new Paragraph($"{owner!.FirstName} {owner.LastName}, Business Owner", normalFont));
                    document.Add(new Paragraph("AND", normalFont) { Alignment = Element.ALIGN_CENTER });
                    document.Add(new Paragraph($"{investor!.FirstName} {investor.LastName}, Investor", normalFont));
                    document.Add(Chunk.NEWLINE);

                    // تفاصيل الصفقة المحدثة
                    document.Add(new Paragraph("CURRENT AGREEMENT TERMS:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                    document.Add(Chunk.NEWLINE);

                    document.Add(new Paragraph($"1. BUSINESS: {deal.BusinessName}", normalFont));
                    document.Add(new Paragraph($"2. INVESTMENT AMOUNT: {deal.OfferMoney:C}", normalFont));
                    document.Add(new Paragraph($"3. EQUITY PERCENTAGE: {deal.OfferDeal}%", normalFont));
                    document.Add(new Paragraph($"4. DURATION: {deal.DurationInMonths} months", normalFont));
                    document.Add(new Paragraph($"6. MANUFACTURING COST: {deal.ManufacturingCost:C} per unit", normalFont));

                    if (contractType == "Renewal")
                    {
                        document.Add(Chunk.NEWLINE);
                        document.Add(new Paragraph("RENEWAL TERMS:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                        document.Add(new Paragraph($"This agreement represents a renewal of the previous investment for an additional {deal.DurationInMonths} months.", normalFont));
                        document.Add(new Paragraph($"All original terms and conditions remain in effect unless specifically modified herein.", normalFont));
                    }
                    else if (contractType == "Amendment")
                    {
                        document.Add(Chunk.NEWLINE);
                        document.Add(new Paragraph("AMENDMENT DETAILS:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                        document.Add(new Paragraph($"This amendment modifies the terms of the original agreement.", normalFont));
                        document.Add(new Paragraph($"All terms not specifically modified herein remain in full force and effect.", normalFont));
                    }

                    // معلومات الدفع إذا كان هناك تغيير في المبلغ
                    if (deal.ChangeAmountDifference.HasValue && Math.Abs(deal.ChangeAmountDifference.Value) > 0.01m)
                    {
                        document.Add(Chunk.NEWLINE);
                        document.Add(new Paragraph("PAYMENT ADJUSTMENT:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                        if (deal.ChangeAmountDifference > 0)
                        {
                            document.Add(new Paragraph($"Additional payment of {deal.ChangeAmountDifference:C} paid by investor on {deal.ChangePaymentProcessedAt:MM/dd/yyyy}.", normalFont));
                        }
                        else
                        {
                            document.Add(new Paragraph($"Refund of {Math.Abs(deal.ChangeAmountDifference.Value):C} processed to investor on {deal.ChangePaymentProcessedAt:MM/dd/yyyy}.", normalFont));
                        }
                    }

                    // التوقيعات
                    document.Add(Chunk.NEWLINE);
                    document.Add(new Paragraph("ELECTRONIC SIGNATURES:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                    document.Add(new Paragraph($"Business Owner: {owner.FirstName} {owner.LastName} - {DateTime.UtcNow:MM/dd/yyyy}", normalFont));
                    document.Add(new Paragraph($"Investor: {investor.FirstName} {investor.LastName} - {DateTime.UtcNow:MM/dd/yyyy}", normalFont));

                    // معلومات التحقق
                    document.Add(Chunk.NEWLINE);
                    var documentHash = CalculateDocumentHash(deal, owner, investor);
                    document.Add(new Paragraph($"Document Hash: {documentHash}", smallFont) { Alignment = Element.ALIGN_CENTER });
                    document.Add(new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC", smallFont) { Alignment = Element.ALIGN_CENTER });

                    document.Close();
                }

                deal.ContractDocumentUrl = $"/Contracts/{fileName}";
                deal.ContractHash = CalculateDocumentHash(deal, owner, investor);
            }

            // ========== Business Logic Methods ==========

            private async Task ApplyChangesToDeal(Deal deal, DealChangeRequest changeRequest)
            {
                var requestedValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    changeRequest.RequestedValues, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // تطبيق التغييرات
                if (requestedValues.TryGetValue("BusinessName", out var businessNameObj) && businessNameObj is System.Text.Json.JsonElement businessNameElement)
                    deal.BusinessName = businessNameElement.GetString();

                if (requestedValues.TryGetValue("Description", out var descriptionObj) && descriptionObj is System.Text.Json.JsonElement descriptionElement)
                    deal.Description = descriptionElement.GetString();

                if (requestedValues.TryGetValue("OfferMoney", out var OfferMoneyObj) && OfferMoneyObj is System.Text.Json.JsonElement OfferMoneyElement)
                    deal.OfferMoney = OfferMoneyElement.GetDecimal();

                if (requestedValues.TryGetValue("OfferDeal", out var equityObj) && equityObj is System.Text.Json.JsonElement equityElement)
                    deal.OfferDeal = equityElement.GetDecimal();

                if (requestedValues.TryGetValue("DurationInMonths", out var durationObj) && durationObj is System.Text.Json.JsonElement durationElement)
                    deal.DurationInMonths = durationElement.GetInt32();

                if (requestedValues.TryGetValue("ManufacturingCost", out var costObj) && costObj is System.Text.Json.JsonElement costElement)
                    deal.ManufacturingCost = costElement.GetDecimal();
            }

            private async Task ProcessRefundToInvestor(Deal deal, decimal refundAmount, DealChangeRequest changeRequest)
            {
                try
                {
                    var refundService = new RefundService();
                    var refundOptions = new RefundCreateOptions
                    {
                        PaymentIntent = deal.PaymentIntentId,
                        Amount = (long)(refundAmount * 100),
                        Reason = "requested",
                        Metadata = new Dictionary<string, string>
                {
                    { "DealId", deal.Id.ToString() },
                    { "ChangeRequestId", changeRequest.Id.ToString() },
                    { "RefundType", "DealChange" }
                }
                    };

                    var refund = await refundService.CreateAsync(refundOptions);

                    // تسجيل الاسترداد
                    var refundLog = new PaymentRefundLog
                    {
                        OrderId = deal.Id,
                        RefundAmount = refundAmount,
                        RefundId = refund.Id,
                        RefundStatus = refund.Status,
                        RefundCreated = DateTime.UtcNow
                    };

                    await _unitOfWork.PaymentRefundLog.AddAsync(refundLog);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing refund for deal {deal.Id}");
                    throw;
                }
            }

            // ========== Notification Methods ==========

            private async Task SendChangeCompletedNotifications(Deal deal, bool paymentRequired, decimal paymentAmount)
            {
                string paymentInfo = paymentRequired ? $" Payment of {paymentAmount:C} has been processed." : "";

                var investorMessage = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.InvestorId!,
                    MessageText = $"Changes to deal '{deal.BusinessName}' have been completed successfully.{paymentInfo} New contract version {deal.ContractVersion} is available.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                var ownerMessage = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.AuthorId,
                    MessageText = $"Changes to deal '{deal.BusinessName}' have been completed successfully.{paymentInfo} New contract version {deal.ContractVersion} is available.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
                await _unitOfWork.Complete();
        }

        private List<ProductPicture> ConvertDealPicturesToProductPictures(List<string> dealPictures)
        {
            if (dealPictures == null || !dealPictures.Any())
            {
                return new List<ProductPicture>();
            }

            return dealPictures.Select(p => new ProductPicture
            {
                PictureUrl = p
            }).ToList();
        }

        #endregion
    }
}