using FirebaseAdmin.Messaging;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DealsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DealsController> _logger;
        private const string _adminUserId = "bba1816c-b7a5-49cb-b282-9895bffde438";

        public DealsController(UserManager<AppUser> userManager, IWebHostEnvironment webHostEnvironment, IUnitOfWork unitOfWork, ILogger<DealsController> logger)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region CRUD Endpoints

        /// <summary>
        /// Add a new deal (Only Business Owners allowed).
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddNewDeal([FromHeader(Name = "Authorization")] string authorizationHeader, [FromForm] AddDealDTO deal)
        {
            // Validate input model
            if (deal == null || !ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid deal data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Authenticate and authorize the user
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only business owners can add deals." });
            }

            // Ensure Category exists
            var category = await _unitOfWork.Category.GetByIdAsync(deal.CategoryId);
            if (category == null)
            {
                return BadRequest(new { Message = "Invalid category ID." });
            }

            // Ensure the Pictures are provided (not null or empty)
            if (deal.Pictures == null || deal.Pictures.Count == 0)
            {
                return BadRequest(new { Message = "At least one image must be provided." });
            }

            // ✅ Ensure directory for images exists
            var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/images/Deals");

            List<ProductPicture> dealPictures = new List<ProductPicture>();  // Use ProductPicture objects instead of strings
            List<string> savedPicturePaths = [];
            try
            {
                // ✅ Save Additional Product Pictures (if any)
                if (deal.Pictures?.Any() == true)
                {
                    savedPicturePaths = await _unitOfWork.FileService.SaveFilesAsync(deal.Pictures, folderPath);
                    dealPictures.AddRange(savedPicturePaths.Select(path => new ProductPicture
                    {
                        PictureUrl = path  // You can store file path or URL here
                    }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error while saving images.", Error = ex.Message });
            }

            // Create Deal Object
            var newDeal = new Deal
            {
                BusinessName = deal.BusinessName,
                Description = deal.Description,
                OfferMoney = deal.OfferMoney,
                OfferDeal = deal.OfferDeal,
                Pictures = savedPicturePaths,
                CategoryId = deal.CategoryId,
                AuthorId = currentUser.Id,
                ManufacturingCost = deal.ManufacturingCost,
                EstimatedPrice = deal.EstimatedPrice,
                IsApproved = false
            };

            await _unitOfWork.Deal.AddAsync(newDeal);
            await _unitOfWork.Complete();

            // Send notification to admin
            await SendNotificationToAllAdminsAsync(
                newDeal.Id,
                currentUser.Id,
                $"New added deal awaiting approval: {newDeal.BusinessName}.",
                MessageType.AdminApprovalRequired
                );

            await _unitOfWork.Complete();

            return Ok(new { Message = "Deal added successfully and is awaiting approval." });
        }


        [HttpGet("GetAllDealsForSpecificBusinessOwner")]
        public async Task<IActionResult> GetAllDealsForAuthor([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only business owners can view their deals." });
            }

            var deals = await _unitOfWork.Deal.GetAllDealsForSpecificAuthor(currentUser.Id);

            var businessOwnerResponse = new BusinessOwnerDealsResponse
            {
                BusinessOwnerId = currentUser.Id,
                BusinessOwnerName = $"{currentUser.FirstName ?? "Unknown"} {currentUser.LastName ?? ""}".Trim(),
                Deals = deals.Select(deal => new DealResponse
                {
                    DealId = deal.Id,
                    BusinessName = deal.BusinessName,
                    Description = deal.Description,
                    OfferDeal = deal.OfferDeal,
                    CategoryId = deal.CategoryId,
                    CategoryName = deal.Category?.Name ?? "Uncategorized",
                    OfferMoney = deal.OfferMoney,
                    ManufacturingCost = deal.ManufacturingCost,
                    EstimatedPrice = deal.EstimatedPrice,
                    Pictures = deal.Pictures?.Select(picture => $"https://innova-hub.premiumasp.net{picture}").ToList() ?? new List<string>(),
                    IsApproved = deal.IsApproved,
                }).ToList()
            };

            return Ok(businessOwnerResponse);
        }


        [HttpGet("GetDealBYId/{Id}")]
        public async Task<IActionResult> GetDealBYId(int Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid deal data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Retrieve deal by Id
            var deal = await _unitOfWork.Deal.GetDealByIdWithAuthorAndCategory(Id);

            // Handle case when deal is not found
            if (deal == null)
            {
                return NotFound(new { Message = "Deal not found." });
            }

            // Map deal to response DTO
            var dealResponse = new GetAllDealsForAuthorResponse()
            {
                DealId = deal.Id,
                BusinessOwnerId = deal.AuthorId,
                BusinessOwnerName = $"{deal.Author?.FirstName ?? "Unknown"} {deal.Author?.LastName ?? ""}".Trim(),
                BusinessOwnerPictureUrl = $"https://innova-hub.premiumasp.net{deal.Author?.ProfileImageUrl ?? "Not Determined"}",
                Description = deal.Description,
                BusinessName = deal.BusinessName,
                CategoryId = deal.CategoryId,
                CategoryName = deal.Category?.Name ?? "Uncategorized",
                OfferDeal = deal.OfferDeal,
                OfferMoney = deal.OfferMoney,
                ManufacturingCost = deal.ManufacturingCost,
                EstimatedPrice = deal.EstimatedPrice,
                Pictures = deal.Pictures?.Select(picture => $"https://innova-hub.premiumasp.net{picture}").ToList() ?? new List<string>(),
                IsApproved = deal.IsApproved,
                ApprovedAt = deal.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Approved"
            };

            return Ok(dealResponse);
        }

        [HttpPatch("EditDeal/{dealId}")]
        public async Task<IActionResult> UpdateDeal(
     [FromHeader(Name = "Authorization")] string authorizationHeader,
     int dealId,
     [FromForm] UpdateDealDTO dealDTO)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Retrieve the existing deal
            var existingDeal = await _unitOfWork.Deal.GetByIdAsync(dealId);
            if (existingDeal == null)
            {
                return NotFound(new { Message = "Deal not found." });
            }

            // Check if the current user is the Business Owner of the deal
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);
            bool isOwner = existingDeal.AuthorId == userId;

            if (!isOwner && !isAdmin)
            {
                return Unauthorized(new { Message = "Only the business owner and admins can update this deal." });
            }

            if (dealDTO == null || !ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid deal data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            if (existingDeal.InvestorId != null && isOwner)
            {
                // Check if there's already a pending change request for this deal
                var existingRequest = await _unitOfWork.DealChangeRequest.GetPendingRequestForDealAsync(dealId);
                if (existingRequest != null)
                {
                    return BadRequest(new
                    {
                        Message = "There is already a pending change request for this deal. Please wait for it to be processed.",
                        ChangeRequestId = existingRequest.Id
                    });
                }

                // Store original deal values
                var originalValues = new
                {
                    BusinessName = existingDeal.BusinessName,
                    Description = existingDeal.Description,
                    OfferMoney = existingDeal.OfferMoney,
                    OfferDeal = existingDeal.OfferDeal,
                    CategoryId = existingDeal.CategoryId,
                    ManufacturingCost = existingDeal.ManufacturingCost,
                    EstimatedPrice = existingDeal.EstimatedPrice,
                    DurationInMonths = existingDeal.DurationInMonths
                };

                // Store requested deal values
                var requestedValues = new
                {
                    BusinessName = dealDTO.BusinessName,
                    Description = dealDTO.Description,
                    OfferMoney = dealDTO.OfferMoney,
                    OfferDeal = dealDTO.OfferDeal,
                    CategoryId = dealDTO.CategoryId,
                    ManufacturingCost = dealDTO.ManufacturingCost,
                    EstimatedPrice = dealDTO.EstimatedPrice,
                    DurationInMonths = dealDTO.DurationInMonths
                };

                // Create a change request
                var changeRequest = new DealChangeRequest
                {
                    DealId = dealId,
                    RequestedById = userId,
                    OriginalValues = System.Text.Json.JsonSerializer.Serialize(originalValues),
                    RequestedValues = System.Text.Json.JsonSerializer.Serialize(requestedValues),
                    Notes = "Deal update requested by business owner.",
                    RequestDate = DateTime.UtcNow,
                    Status = ChangeRequestStatus.Pending
                };

                await _unitOfWork.DealChangeRequest.AddAsync(changeRequest);
                await _unitOfWork.Complete();

                // Send notification to the investor
                var investorNotification = new DealMessage
                {
                    DealId = dealId,
                    SenderId = userId,
                    RecipientId = existingDeal.InvestorId!,
                    MessageText = $"The business owner of '{existingDeal.BusinessName}' has proposed changes to the deal. Please review and respond.",
                    ChangeRequestId = changeRequest.Id,
                    IsRead = false,
                    MessageType = MessageType.EditRequested,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Deal update requested. The changes will be applied after approval from investor.",
                    ChangeRequestId = changeRequest.Id
                });
            }
            else
            {
                // Direct update for deals without active investments or for admin users

                // Update Deal Fields
                existingDeal.BusinessName = dealDTO.BusinessName ?? existingDeal.BusinessName;
                existingDeal.Description = dealDTO.Description ?? existingDeal.Description;
                existingDeal.OfferMoney = dealDTO.OfferMoney ?? existingDeal.OfferMoney;
                existingDeal.OfferDeal = dealDTO.OfferDeal ?? existingDeal.OfferDeal;
                existingDeal.CategoryId = dealDTO.CategoryId ?? existingDeal.CategoryId;
                existingDeal.ManufacturingCost = dealDTO.ManufacturingCost ?? existingDeal.ManufacturingCost;
                existingDeal.EstimatedPrice = dealDTO.EstimatedPrice ?? existingDeal.EstimatedPrice;
                existingDeal.OfferDeal = dealDTO.OfferDeal ?? existingDeal.OfferDeal;
                existingDeal.DurationInMonths = dealDTO.DurationInMonths ?? existingDeal.DurationInMonths;
                existingDeal.IsApproved = isAdmin || existingDeal.Status != DealStatus.OwnerAccepted;

                // Process Image Uploads
                if (dealDTO.Pictures != null && dealDTO.Pictures.Any())
                {
                    // Delete old images if necessary
                    foreach (var oldPicture in existingDeal.Pictures)
                    {
                        _unitOfWork.FileService.DeleteFile(oldPicture);
                    }

                    // Save new images
                    string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Deals");
                    _unitOfWork.FileService.EnsureDirectory(folderPath);
                    existingDeal.Pictures = await _unitOfWork.FileService.SaveFilesAsync(dealDTO.Pictures, folderPath);
                }

                // Save changes to the database
                await _unitOfWork.Complete();

                return Ok(new { Message = "Deal updated successfully." });
            }
        }

        [HttpPost("respond-to-deal-change")]
        public async Task<IActionResult> RespondToDealChange(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromBody] RespondToDealChangeDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var changeRequest = await _unitOfWork.DealChangeRequest.GetWithDetailsAsync(request.ChangeRequestId);
            if (changeRequest == null)
                return NotFound(new { Message = "Change request not found." });

            var deal = changeRequest.Deal;
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // التحقق من التفويض
            if (userId != deal.InvestorId)
                return Unauthorized(new { Message = "You are not authorized to respond to this change request." });

            if (changeRequest.Status != ChangeRequestStatus.Pending)
                return BadRequest(new { Message = "This change request has already been processed." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            if (request.IsApproved)
            {
                changeRequest.Status = ChangeRequestStatus.Approved;
                changeRequest.ApprovedById = userId;
                changeRequest.ApprovalDate = DateTime.UtcNow;

                await _unitOfWork.DealChangeRequest.UpdateAsync(changeRequest);

                // حساب الفرق في مبلغ الاستثمار
                var originalValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    changeRequest.OriginalValues, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var requestedValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    changeRequest.RequestedValues, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                decimal originalAmount = GetDecimalFromJson(originalValues, "OfferMoney") ?? deal.OfferMoney;
                decimal newAmount = GetDecimalFromJson(requestedValues, "OfferMoney") ?? deal.OfferMoney;
                decimal amountDifference = newAmount - originalAmount;

                // إذا لا يوجد تغيير في المبلغ، تطبيق التغييرات مباشرة
                if (Math.Abs(amountDifference) < 0.01m)
                {
                    await ApplyChangesDirectly(deal, changeRequest);

                    return Ok(new
                    {
                        Message = "Changes approved and applied successfully. New contract generated.",
                        ChangeRequestId = changeRequest.Id,
                        RequiresPayment = false,
                        NewContractVersion = deal.ContractVersion
                    });
                }
                else
                {
                    // يتطلب دفع إضافي أو استرداد
                    string paymentDirection = amountDifference > 0 ? "investor_pays" : "refund_to_investor";
                    string requiredPayer = amountDifference > 0 ? "investor" : "business owner";

                    // إشعار بالحاجة للدفع
                    var paymentNotification = new DealMessage
                    {
                        DealId = deal.Id,
                        SenderId = userId,
                        RecipientId = amountDifference > 0 ? deal.InvestorId! : deal.AuthorId,
                        MessageText = amountDifference > 0
                            ? $"Your deal changes for '{deal.BusinessName}' have been approved. Additional payment of {Math.Abs(amountDifference):C} is required to complete the changes."
                            : $"Your deal changes for '{deal.BusinessName}' have been approved. A refund of {Math.Abs(amountDifference):C} will be processed.",
                        IsRead = false,
                        MessageType = MessageType.General,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.InvestmentMessage.AddAsync(paymentNotification);
                    await _unitOfWork.Complete();

                    return Ok(new
                    {
                        Message = "Changes approved. Payment processing required to complete changes.",
                        ChangeRequestId = changeRequest.Id,
                        RequiresPayment = true,
                        PaymentAmount = Math.Abs(amountDifference),
                        PaymentDirection = paymentDirection,
                        NextStep = $"Payment required from {requiredPayer}"
                    });
                }
            }

            else
            {
                // رفض التغييرات
                changeRequest.Status = ChangeRequestStatus.Rejected;
                changeRequest.RejectionReason = request.RejectionReason;

                await _unitOfWork.DealChangeRequest.UpdateAsync(changeRequest);

                var rejectionNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = changeRequest.RequestedById,
                    MessageText = $"Your requested changes to deal '{deal.BusinessName}' have been rejected. {request.RejectionReason}",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(rejectionNotification);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Change request rejected.",
                    ChangeRequestId = changeRequest.Id
                });
            }
        }

        [HttpDelete("{dealId}")]
        public async Task<IActionResult> DeleteDeal([FromHeader(Name = "Authorization")] string authorizationHeader, int dealId)
        {
            // Authenticate and authorize the user
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);

            // Retrieve the deal
            var deal = await _unitOfWork.Deal.GetByIdAsync(dealId);
            if (deal == null)
            {
                return NotFound(new { Message = "Deal not found." });
            }

            bool isOwner = deal.AuthorId == userId;
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);

            // Ensure the authenticated user owns the deal
            if (!isOwner && !isAdmin)
            {
                return Unauthorized(new { Message = "You are not the admin or the owner of this deal." });
            }

            if (deal.InvestorId != null && isOwner)
            {
                // Check if there's already a pending delete request for this deal
                var existingRequest = await _unitOfWork.DealDeleteRequest.GetPendingRequestForDealAsync(dealId);
                if (existingRequest != null)
                {
                    return BadRequest(new
                    {
                        Message = "There is already a pending delete request for this deal. Please wait for it to be processed.",
                        ChangeRequestId = existingRequest.Id
                    });
                }

                var currentUser = await _unitOfWork.Auth.GetUserById(userId);

                // Create a delete request
                var deleteRequest = new DealDeleteRequest
                {
                    DealId = dealId,
                    RequestedById = userId,
                    Notes = "Deal deletion requested by business owner.",
                    RequestDate = DateTime.UtcNow,
                    Status = DeleteRequestStatus.Pending
                };

                await _unitOfWork.DealDeleteRequest.AddAsync(deleteRequest);
                await _unitOfWork.Complete();

                // Send notification to the investor
                var investorNotification = new DealMessage
                {
                    DealId = dealId,
                    SenderId = userId,
                    RecipientId = deal.InvestorId!,
                    MessageText = $"The business owner of '{deal.BusinessName}' wants to delete the deal. Please review and respond.",
                    DeletionRequestId = deleteRequest.Id,
                    IsRead = false,
                    MessageType = MessageType.EditRequested,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Deal deletion requested. The deletion will be applied after approval from investor.",
                    ChangeRequestId = deleteRequest.Id
                });
            }

            else
            // Delete associated images
            if (deal.Pictures != null)
            {
                foreach (var picture in deal.Pictures)
                {
                    _unitOfWork.FileService.DeleteFile(picture);
                }
            }

            // Perform deletion
            bool isDeleted = await _unitOfWork.Deal.DeleteAsync(dealId);
            if (!isDeleted)
            {
                return BadRequest(new { Message = "Failed to delete deal." });
            }

            // Save changes
            await _unitOfWork.Complete();

            return Ok(new { Message = "Deal deleted successfully." });
        }

        [HttpPost("respond-to-deal-deletion")]
        public async Task<IActionResult> RespondToDealDeletion(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromBody] RespondToDealDeletionDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deleteRequest = await _unitOfWork.DealDeleteRequest.GetWithDetailsAsync(request.DeletionRequestId);
            if (deleteRequest == null)
                return NotFound(new { Message = "Deletion request not found." });

            var deal = deleteRequest.Deal;
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Check if user is an investor for this deal
            if (userId != deal.InvestorId)
                return Unauthorized(new { Message = "You are not authorized to respond to this deletion request." });

            if (deleteRequest.Status != DeleteRequestStatus.Pending)
                return BadRequest(new { Message = "This deletion request has already been processed." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            if (request.IsApproved)
            {
                // Approve the changes
                deleteRequest.Status = DeleteRequestStatus.Approved;
                deleteRequest.ApprovedById = userId;
                deleteRequest.ApprovalDate = DateTime.UtcNow;

                await _unitOfWork.DealDeleteRequest.UpdateAsync(deleteRequest);

                // Notify the requester
                var requesterNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deleteRequest.RequestedById,
                    MessageText = $"Your requested deletion to the deal '{deal.BusinessName}' have been approved and applied.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(requesterNotification);
                await _unitOfWork.Complete();

                // Apply the deletion to the deal
                if (deal.Pictures != null)
                {
                    foreach (var picture in deal.Pictures)
                    {
                        _unitOfWork.FileService.DeleteFile(picture);
                    }
                }

                // Perform deletion
                bool isDeleted = await _unitOfWork.Deal.DeleteAsync(deal.Id);
                if (!isDeleted)
                {
                    return BadRequest(new { Message = "Failed to delete deal." });
                }

                // Save changes
                await _unitOfWork.Complete();

                return Ok(new { Message = "Deal deleted successfully." });
            }

            else
            {
                // Reject the deletion
                deleteRequest.Status = DeleteRequestStatus.Rejected;
                deleteRequest.RejectionReason = request.RejectionReason;

                await _unitOfWork.DealDeleteRequest.UpdateAsync(deleteRequest);

                // Notify the requester
                var requesterNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deleteRequest.RequestedById,
                    MessageText = $"Your requested deletion to the deal '{deal.BusinessName}' have been rejected. {request.RejectionReason}",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(requesterNotification);
                await _unitOfWork.Complete();
            }

            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = request.IsApproved
                    ? "Deletion request approved and applied successfully."
                    : "Deletion request rejected."
            });
        }

        [HttpGet("GetAllDeals")]
        public async Task<IActionResult> GetAllDeals()
        {
            var deals = await _unitOfWork.Deal.GetAllDealsAsync(); // ✅ Corrected method call

            var response = deals.Select(deal => new GetAllDealsForAuthorResponse
            {
                DealId = deal.Id,
                BusinessOwnerId = deal.AuthorId,
                BusinessOwnerName = $"{deal.Author?.FirstName ?? "Unknown"} {deal.Author?.LastName ?? ""}".Trim(),
                BusinessOwnerPictureUrl = $"https://innova-hub.premiumasp.net{deal.Author?.ProfileImageUrl ?? "Not Determined"}",
                BusinessName = deal.BusinessName,
                Description = deal.Description,
                OfferDeal = deal.OfferDeal,
                CategoryId = deal.CategoryId,
                CategoryName = deal.Category?.Name ?? "Uncategorized",
                OfferMoney = deal.OfferMoney,
                ManufacturingCost = deal.ManufacturingCost,
                EstimatedPrice = deal.EstimatedPrice,
                IsApproved = deal.IsApproved,
                ApprovedAt = deal.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Approved",
                Pictures = deal.Pictures?.Select(picture => $"https://innova-hub.premiumasp.net{picture}").ToList() ?? new List<string>()
            }).ToList();

            return Ok(response);
        }

        // Accept an offer (by investor)
        [HttpPost("accept-offer")]
        public async Task<IActionResult> AcceptOffer(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] AcceptOfferDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Verify the user is an investor
            if (!await _unitOfWork.Auth.IsInvestor(userId))
                return Unauthorized(new { Message = "Only investors can accept offers." });

            var deal = await _unitOfWork.Deal.GetByIdAsync(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            if (!deal.IsApproved)
                return BadRequest(new { Message = "Deal is not approved by admin." });

            if(deal.InvestorId != null)
                return Unauthorized(new { Message = "Deal has already been linked with an investor" });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            deal.Status = DealStatus.Pending;
            //deal.InvestorId = currentUser.Id;
            //deal.IsVisible = false;

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            // Send notification to business owner
            var ownerNotification = new DealMessage
            {
                DealId = deal.Id,
                SenderId = userId,
                RecipientId = deal.AuthorId,
                MessageText = $"{currentUser.FirstName} {currentUser.LastName} has accepted your offer for the {deal.BusinessName} project with an investment amount of {deal.OfferMoney} and a {deal.OfferDeal}% equity share.. " +
                              "Please review and respond.",
                IsRead = false,
                MessageType = MessageType.OfferAccepted,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = "Offer accepted successfully. Awaiting owner's response.",
                DealId = deal.Id
            });
        }

        // Discuss an offer (by investor)
        [HttpPost("discuss-offer")]
        public async Task<IActionResult> DiscussOffer(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] DiscussOfferDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            if (!await _unitOfWork.Auth.IsInvestor(userId))
                return Unauthorized(new { Message = "Only investors can discuss offers." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            if (!deal.IsApproved)
                return BadRequest(new { Message = "Deal is not approved by admin." });

            if (deal.InvestorId != null)
                return Unauthorized(new { Message = "Deal has already been linked with an investor" });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            //deal.InvestorId = userId;
            //deal.IsVisible = false;

            //await _unitOfWork.Deal.UpdateAsync(deal);
            //await _unitOfWork.Complete();

            // Create message
            var message = new DealMessage
            {
                SenderId = userId,
                RecipientId = deal.AuthorId,
                MessageText = $"{currentUser.FirstName} {currentUser.LastName} has requested a discussion regarding the following: {request.Message}",
                IsRead = false,
                DealId = request.DealId,
                MessageType = MessageType.OfferDiscussion,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(message);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Discussion message sent successfully." });
        }

        // Owner responds to investment offer
        [HttpPost("respond-to-offer")]
        public async Task<IActionResult> RespondToOffer(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] RespondToOfferDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Verify that the user is the deal owner
            if (deal.AuthorId != userId)
                return Unauthorized(new { Message = "Only the business owner can respond to this offer." });

            var investor = await _unitOfWork.Auth.GetUserById(request.InvestorId);
            if(investor == null)
                return BadRequest(new { Message = "Investor not found." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            if (request.IsAccepted)
            {
                deal.InvestorId = request.InvestorId;
                deal.Status = DealStatus.OwnerAccepted;
                deal.AcceptedByOwnerAt = DateTime.UtcNow;
                deal.IsVisible = false;

                await _unitOfWork.Deal.UpdateAsync(deal);
                await _unitOfWork.Complete();

                // Send notification to admin
                await SendNotificationToAllAdminsAsync(
                    deal.Id,
                    userId,
                    $"New deal awaiting approval: {deal.BusinessName}. Business owner has accepted an deal offer of {deal.OfferMoney:C} for {deal.OfferDeal}% equity.",
                    MessageType.AdminApprovalRequired
                    );

                // Send notification to investor
                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = request.InvestorId,
                    MessageText = "Your deal offer has been accepted by the business owner. Waiting for admin approval.",
                    IsRead = false,
                    MessageType = MessageType.OfferAccepted,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.Complete();
            }

            else
            {
                deal.Status = DealStatus.Rejected;
                deal.InvestorId = null;
                deal.IsVisible = true;

                // Send rejection notification to investor
                var rejectionMessage = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = request.InvestorId,
                    MessageText = "Your deal offer has been rejected by the business owner.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(rejectionMessage);
                await _unitOfWork.Complete();
            }

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            return Ok(new { Message = request.IsAccepted ? "Offer accepted successfully." : "Offer rejected." });
        }

        // Admin approves deal
        [HttpPost("admin-approval")]
        public async Task<IActionResult> AdminApproval(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] AdminApprovalDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Verify the user is an admin
            if (!await _unitOfWork.Auth.IsAdmin(userId))
                return Unauthorized(new { Message = "Only admins can approve deals." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            if (deal.Status != DealStatus.OwnerAccepted)
                return BadRequest(new { Message = "Deal is not in a state that can be approved." });

            if (request.IsApproved)
            {
                deal.Status = DealStatus.AdminApproved;
                deal.ApprovedByAdminAt = DateTime.UtcNow;
                deal.IsVisible = false;

                await NotifyPartiesOfApproval(deal, userId);
            }
            else
            {
                deal.Status = DealStatus.Rejected;
                deal.IsVisible = true;

                // Notify both parties of rejection
                await NotifyPartiesOfRejection(deal, request.RejectionReason, userId);
                deal.InvestorId = null;
            }

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            return Ok(new { Message = request.IsApproved ? "Deal approved successfully." : "Deal rejected." });
        }

        [HttpGet("investor-deals")]
        public async Task<IActionResult> GetInvestorInvestments(
    [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            if (!await _unitOfWork.Auth.IsInvestor(userId))
                return Unauthorized(new { Message = "Only investors can see this information." });

            var deals = await _unitOfWork.Deal.GetDealsByInvestorId(userId);

            var dealDTOs = deals.Select(i => {
                // Calculate total earnings
                decimal totalEarnings = i.ProfitDistributions?.Sum(p => p.InvestorShare) ?? 0;

                // Calculate percentage change from previous period
                var latestProfit = i.ProfitDistributions?.OrderByDescending(p => p.DistributionDate).FirstOrDefault();
                var previousProfit = i.ProfitDistributions?.OrderByDescending(p => p.DistributionDate).Skip(1).FirstOrDefault();

                decimal percentageChange = 0;
                if (previousProfit != null && previousProfit.InvestorShare > 0)
                {
                    percentageChange = ((latestProfit?.InvestorShare ?? 0) - previousProfit.InvestorShare)
                                      / previousProfit.InvestorShare * 100;
                }

                string formattedChange = percentageChange >= 0
                    ? $"+{percentageChange:F2}%"
                    : $"{percentageChange:F2}%";

                return new DealDTO
                {
                    DealId = i.Id,
                    ProjectName = i.BusinessName,
                    OwnerName = $"{i.Author.FirstName} {i.Author.LastName}",
                    OfferMoney = i.OfferMoney,
                    OfferDeal = i.OfferDeal,
                    Status = i.Status.ToString() ?? "Unknown",
                    CreatedAt = i.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    TotalProfit = i.ProfitDistributions?.Sum(p => p.InvestorShare) ?? 0,
                    LastDistribution = i.ProfitDistributions?.OrderByDescending(p => p.DistributionDate).FirstOrDefault()?.DistributionDate,
                    DurationInMonths = i.DurationInMonths,
                    StartDate = i.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Determined",
                    EndDate = i.ScheduledEndDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Determined",
                    RemainingDays = i.ScheduledEndDate.HasValue ?
                        Math.Max(0, (int)(i.ScheduledEndDate.Value - DateTime.UtcNow).TotalDays) : null
                };
            }).ToList();

            return Ok(dealDTOs);
        }


        // Get deals for business owner
        [HttpGet("owner-deals")]
        public async Task<IActionResult> GetOwnerInvestments(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
            if (currentUser == null)
                return Unauthorized(new { Message = "Only business owners can see this information." });

            var deals = await _unitOfWork.Deal.GetDealsByOwnerId(userId);

            var investmentDTOs = deals.Select(i => {
                // Calculate total earnings
                decimal totalEarnings = i.ProfitDistributions?.Sum(p => p.OwnerShare) ?? 0;

                // Calculate percentage change from previous period
                var latestProfit = i.ProfitDistributions?.OrderByDescending(p => p.DistributionDate).FirstOrDefault();
                var previousProfit = i.ProfitDistributions?.OrderByDescending(p => p.DistributionDate).Skip(1).FirstOrDefault();

                decimal percentageChange = 0;
                if (previousProfit != null && previousProfit.OwnerShare > 0)
                {
                    percentageChange = ((latestProfit?.OwnerShare ?? 0) - previousProfit.OwnerShare)
                                      / previousProfit.OwnerShare * 100;
                }

                string formattedChange = percentageChange >= 0
                    ? $"+{percentageChange:F2}%"
                    : $"{percentageChange:F2}%";

                return new DealDTO
                {
                    DealId = i.Id,
                    ProjectName = i.BusinessName,
                    //InvestorName = $"{i.Investor.FirstName} {i.Investor.LastName}",
                    OwnerName = $"{i.Author.FirstName} {i.Author.LastName}",
                    OfferMoney = i.OfferMoney,
                    OfferDeal = i.OfferDeal,
                    Status = i.Status.ToString() ?? "Not dealed with investor yet",
                    CreatedAt = i.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    TotalProfit = i.ProfitDistributions?.Sum(p => p.OwnerShare) ?? 0,
                    LastDistribution = i.ProfitDistributions?.OrderByDescending(p => p.DistributionDate).FirstOrDefault()?.DistributionDate,
                    DurationInMonths = i.DurationInMonths,
                    StartDate = i.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Determined",
                    EndDate = i.ScheduledEndDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Determined",
                    RemainingDays = i.ScheduledEndDate.HasValue ?
                        Math.Max(0, (int)(i.ScheduledEndDate.Value - DateTime.UtcNow).TotalDays) : null
                };
            }).ToList();

            return Ok(investmentDTOs);
        }

        // Get messages for an investment
        [HttpGet("messages/{dealId}")]
        public async Task<IActionResult> GetInvestmentMessages(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            int dealId)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Check if user is related to this investment
            if (deal.InvestorId != userId && deal.AuthorId != userId && !await _unitOfWork.Auth.IsAdmin(userId))
                return Unauthorized(new { Message = "You are not authorized to view these messages." });

            var messages = await _unitOfWork.InvestmentMessage.GetMessages(userId, dealId);

            var messageDTOs = messages.Select(m => new InvestmentMessageDTO
            {
                Id = m.Id,
                DealId = m.DealId,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                RecipientName = $"{m.Recipient.FirstName} {m.Recipient.LastName}",//
                MessageText = m.MessageText,
                MessageType = m.MessageType.ToString(),
                ChangeRequestId = m.ChangeRequestId,
                DeletionRequestId = m.DeletionRequestId,
                ProfitDistributionId = m.ProfitDistributionId,
                ContractUrl = m.ContractUrl,
                CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                IsRead = m.IsRead
            }).ToList();

            return Ok(messageDTOs);
        }

        [HttpGet("calculate-profit/{dealId}")]
        public async Task<IActionResult> CalculateProfit(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    int dealId,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Verify this is either an admin, the owner, or the investor
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);
            bool isOwner = deal.AuthorId == userId;
            bool isInvestor = deal.InvestorId == userId;

            if (!isOwner && !isInvestor && !isAdmin)
                return Unauthorized(new { Message = "You are not authorized to calculate profits for this deal." });

            if(!deal.IsProductCreated)
                return BadRequest(new { Message = "Deal not linked to a product" });

            // Default to current month if dates not provided
            var now = DateTime.UtcNow;
            var actualStartDate = startDate ?? new DateTime(now.Year, now.Month, 1);
            var actualEndDate = endDate ?? now;

            try
            {
                var result = await CalculateMonthlyProfit(dealId, actualStartDate, actualEndDate);

                return Ok(new
                {
                    Message = "Profit calculated successfully.",
                    Calculation = result,
                    CanRecord = isOwner || isAdmin
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error calculating profit", Error = ex.Message });
            }
        }

        // Record profit distribution
        [HttpPost("record-profit")]
        public async Task<IActionResult> RecordAutomatedProfitDistribution(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromBody] RecordProfitDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Verify this is either an admin or the business owner
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);
            bool isOwner = deal.AuthorId == userId;

            if (!isOwner && !isAdmin)
                return Unauthorized(new { Message = "Only the business owner or admin can record profit distributions." });

            if (deal.Status != DealStatus.Active)
                return BadRequest(new { Message = "Only active deals can have profit distributions." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);
            try
            {
                var startDate = request.StartDate ?? deal.Product.CreatedAt;
                var endDate = request.EndDate ?? DateTime.UtcNow;

                // Calculate profits from actual sales data
                var calculationResult = await CalculateMonthlyProfit(deal.Id, startDate, endDate);

                // Check if we have profitable data
                if (calculationResult.NetProfit <= 0)
                    return BadRequest(new { Message = "No profit to distribute for this period. Net profit must be greater than zero." });

                // Create profit distribution record from calculated data
                var profitDistribution = new DealProfit
                {
                    DealId = deal.Id,
                    TotalRevenue = calculationResult.TotalRevenue,
                    ManufacturingCost = calculationResult.ManufacturingCost,
                    OtherCosts = calculationResult.OtherCosts,
                    NetProfit = calculationResult.NetProfit,
                    InvestorShare = calculationResult.InvestorShare,
                    OwnerShare = calculationResult.OwnerShare,
                    PlatformFee = calculationResult.PlatformFee,
                    DistributionDate = DateTime.UtcNow,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsPaid = false,
                    IsPending = !isAdmin, // Auto-approve if admin is recording
                    IsApprovedByAdmin = isAdmin, // Auto-approve if admin is recording
                    AdminId = isAdmin ? userId : null,
                    ApprovalDate = isAdmin ? DateTime.UtcNow : null
                };

                await _unitOfWork.InvestmentProfit.AddAsync(profitDistribution);
                await _unitOfWork.Complete();

                // Send notifications (same as original method)
                if (isOwner)
                {
                    await SendNotificationToAllAdminsAsync(
                        deal.Id,
                        userId,
                        $"Business owner has recorded profit of {calculationResult.NetProfit:C} for '{deal.BusinessName}' for period from {startDate.ToString("yyyy-MM-dd")} to {endDate.ToString("yyyy-MM-dd")}. Please verify and approve.",
                        MessageType.ProfitRecorded
                        );

                    // Also notify the investor
                    var investorNotification = new DealMessage
                    {
                        DealId = deal.Id,
                        SenderId = userId,
                        RecipientId = deal.InvestorId!,
                        MessageText = $"Business owner has recorded profit of {calculationResult.NetProfit:C} for period from {startDate.ToString("yyyy-MM-dd")} to {endDate.ToString("yyyy-MM-dd")} based on actual sales data. This is pending admin verification.",
                        ProfitDistributionId = profitDistribution.Id,
                        IsRead = false,
                        MessageType = MessageType.General,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                    await _unitOfWork.Complete();
                }

                else // Admin recorded, notify both parties
                {
                    // Notify investor
                    var investorNotification = new DealMessage
                    {
                        DealId = deal.Id,
                        SenderId = userId,
                        RecipientId = deal.InvestorId!,
                        MessageText = $"Admin has recorded and approved profit of {calculationResult.NetProfit:C} for period {startDate.ToString("yyyy-MM-dd")} to {endDate.ToString("yyyy-MM-dd")} based on actual sales data. Your share: {calculationResult.InvestorShare:C}",
                        ProfitDistributionId = profitDistribution.Id,
                        IsRead = false,
                        MessageType = MessageType.General,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Notify business owner
                    var ownerNotification = new DealMessage
                    {
                        DealId = deal.Id,
                        SenderId = userId,
                        RecipientId = deal.AuthorId!,
                        MessageText = $"Admin has recorded and approved profit of {calculationResult.NetProfit:C} for period {startDate.ToString("yyyy-MM-dd")} to {endDate.ToString("yyyy-MM-dd")} based on actual sales data. Your share: {calculationResult.OwnerShare:C}",
                        ProfitDistributionId = profitDistribution.Id,
                        IsRead = false,
                        MessageType = MessageType.General,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                    await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                    await _unitOfWork.Complete();
                }

                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = isAdmin ? "Profit distribution automatically calculated and approved." : "Profit distribution automatically calculated and awaiting admin approval.",
                    ProfitDistributionId = profitDistribution.Id,
                    IsPending = profitDistribution.IsPending,
                    Details = new
                    {
                        TotalRevenue = calculationResult.TotalRevenue,
                        ManufacturingCost = calculationResult.ManufacturingCost,
                        OtherCosts = calculationResult.OtherCosts,
                        NetProfit = calculationResult.NetProfit,
                        InvestorShare = calculationResult.InvestorShare,
                        OwnerShare = calculationResult.OwnerShare,
                        PlatformFee = calculationResult.PlatformFee,
                        UnitsSold = calculationResult.TotalQuantitySold
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error calculating or recording profit", Error = ex.Message });
            }
        }

        [HttpPost("approve-profit")]
        public async Task<IActionResult> ApproveProfitDistribution(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] ApproveProfitDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Ensure user is admin
            if (!await _unitOfWork.Auth.IsAdmin(userId))
                return Unauthorized(new { Message = "Only admins can approve profit distributions." });

            var profitDistribution = await _unitOfWork.InvestmentProfit.GetByIdAsync(request.ProfitDistributionId);
            if (profitDistribution == null)
                return NotFound(new { Message = "Profit distribution not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(profitDistribution.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            if (!profitDistribution.IsPending)
                return BadRequest(new { Message = "This profit distribution has already been processed." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            if (request.IsApproved)
            {
                // Approve the profit distribution
                profitDistribution.IsPending = false;
                profitDistribution.IsApprovedByAdmin = true;
                profitDistribution.AdminId = userId;
                profitDistribution.ApprovalDate = DateTime.UtcNow;

                await _unitOfWork.InvestmentProfit.UpdateAsync(profitDistribution);

                // Notify investor
                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.InvestorId!,
                    MessageText = $"Admin has approved profit distribution of {profitDistribution.NetProfit:C} for period from {profitDistribution.StartDate} to {profitDistribution.EndDate}. Your share: {profitDistribution.InvestorShare:C}",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                // Notify business owner
                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.AuthorId!,
                    MessageText = $"Admin has approved your profit distribution of {profitDistribution.NetProfit:C} for period from {profitDistribution.StartDate} to {profitDistribution.EndDate}. Your share: {profitDistribution.OwnerShare:C}",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.Complete();
            }

            else
            {
                // Reject the profit distribution
                profitDistribution.IsPending = false;
                profitDistribution.IsApprovedByAdmin = false;

                await _unitOfWork.InvestmentProfit.UpdateAsync(profitDistribution);

                // Notify business owner
                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.AuthorId!,
                    MessageText = $"Admin has rejected your profit distribution for period from {profitDistribution.StartDate} to {profitDistribution.EndDate}. {request.RejectionReason}",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.Complete();
            }

            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = request.IsApproved ? "Profit distribution approved successfully." : "Profit distribution rejected.",
                IsApproved = profitDistribution.IsApprovedByAdmin
            });
        }

        [HttpPost("request-termination")]
        public async Task<IActionResult> RequestTermination(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] TerminateDealDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Determine if user is investor or owner
            bool isOwner = deal.AuthorId == userId;
            bool isInvestor = deal.InvestorId == userId;
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);

            if (!isOwner && !isInvestor && !isAdmin)
                return Unauthorized(new { Message = "You are not authorized to terminate this deal." });

            if (deal.Status != DealStatus.Active)
                return BadRequest(new { Message = "Only active deals can be terminated." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            // If admin is terminating, process immediately
            if (isAdmin)
            {
                await TerminateDeal(deal, request.EndReason, request.TerminationNotes, userId);

                // Notify both parties
                var terminationMessage = $"An admin has terminated the deal in '{deal.BusinessName}'. {request.EndReason}. {request.TerminationNotes}";

                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.InvestorId!,
                    MessageText = terminationMessage,
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.AuthorId,
                    MessageText = terminationMessage,
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.Complete();

                return Ok(new
                {
                    Message = "Deal terminated by admin.",
                    TerminationDate = deal.ActualEndDate,
                    Reason = deal.EndReason.ToString()
                });
            }

            // Record termination request
            if (isOwner)
            {
                deal.TerminationRequestedByOwner = true;
                deal.OwnerTerminationRequestDate = DateTime.UtcNow;

                // Notify investor
                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.InvestorId!,
                    MessageText = $"The business owner has requested to terminate the deal in '{deal.BusinessName}'. {request.EndReason}. {request.TerminationNotes}. Please respond to this request.",
                    IsRead = false,
                    MessageType = MessageType.TerminationRequested,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.Complete();
            }
            else // isInvestor
            {
                deal.TerminationRequestedByInvestor = true;
                deal.InvestorTerminationRequestDate = DateTime.UtcNow;

                // Notify owner
                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = deal.AuthorId,
                    MessageText = $"The investor has requested to terminate the deal in '{deal.BusinessName}'. {request.EndReason}. {request.TerminationNotes}. Please respond to this request.",
                    IsRead = false,
                    MessageType = MessageType.TerminationRequested,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.Complete();
            }

            deal.TerminationRequestReason = $"{request.EndReason}: {request.TerminationNotes}";

            // Check if both parties have requested termination
            if (deal.TerminationRequestedByOwner && deal.TerminationRequestedByInvestor)
            {
                // Process the termination
                await TerminateDeal(deal, request.EndReason, request.TerminationNotes ?? "", _adminUserId);

                // Notify both parties
                var terminationMessage = $"The deal in '{deal.BusinessName}' has been terminated by mutual agreement.";

                var investorTerminationNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.InvestorId!,
                    MessageText = terminationMessage,
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                var ownerTerminationNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.AuthorId,
                    MessageText = terminationMessage,
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorTerminationNotification);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerTerminationNotification);
                await _unitOfWork.Complete();
            }

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = "Termination request recorded. Waiting for the other party to respond.",
                BothPartiesRequested = deal.TerminationRequestedByOwner && deal.TerminationRequestedByInvestor
            });
        }

        [HttpPost("respond-to-termination")]
        public async Task<IActionResult> RespondToTermination(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] RespondToTerminationDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(request.DealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Determine if user is investor or owner
            bool isOwner = deal.AuthorId == userId;
            bool isInvestor = deal.InvestorId == userId;

            if (!isOwner && !isInvestor)
                return Unauthorized(new { Message = "You are not authorized to respond to this termination request." });

            // Check if there's a termination request to respond to
            bool hasTerminationRequest = (isOwner && deal.TerminationRequestedByInvestor) ||
                                        (isInvestor && deal.TerminationRequestedByOwner);

            if (!hasTerminationRequest)
                return BadRequest(new { Message = "There is no pending termination request to respond to." });

            var currentUser = await _unitOfWork.Auth.GetUserById(userId);

            if (request.IsApproved)
            {
                // Record approval
                if (isOwner)
                {
                    deal.TerminationRequestedByOwner = true;
                    deal.OwnerTerminationRequestDate = DateTime.UtcNow;
                }
                else // isInvestor
                {
                    deal.TerminationRequestedByInvestor = true;
                    deal.InvestorTerminationRequestDate = DateTime.UtcNow;
                }

                // Process termination as both parties now agree
                await TerminateDeal(deal, DealEndReason.MutualAgreement, "Both parties agreed to termination", userId);

                // Notify both parties
                var terminationMessage = $"The deal in '{deal.BusinessName}' has been terminated by mutual agreement.";

                var investorNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.InvestorId!,
                    MessageText = terminationMessage,
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                var ownerNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.AuthorId,
                    MessageText = terminationMessage,
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorNotification);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerNotification);
                await _unitOfWork.Complete();
            }

            else
            {
                // Record rejection and escalate to admin
                deal.IsTerminationEscalatedToAdmin = true;
                deal.TerminationEscalationDate = DateTime.UtcNow;

                // Notify admin
                await SendNotificationToAllAdminsAsync(
                        deal.Id,
                        userId,
                        $"Termination dispute for deal in '{deal.BusinessName}'. One party requested termination but the other party has rejected. Please review and make a decision.",
                        MessageType.TerminationRequested
                        );

                // Notify the requester
                string requesterRole = isOwner ? "investor" : "business owner";
                string requesterUserId = isOwner ? deal.InvestorId! : deal.AuthorId;

                var requesterNotification = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = userId,
                    RecipientId = requesterUserId,
                    MessageText = $"Your termination request has been rejected by the {(isOwner ? "business owner" : "investor")}. The matter has been sent to admin for review.",
                    IsRead = false,
                    MessageType = MessageType.General,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.InvestmentMessage.AddAsync(requesterNotification);
                await _unitOfWork.Complete();
            }

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = request.IsApproved ?
                    "Termination approved. The deal has been terminated." :
                    "Termination rejected. The matter has been sent to admin for review."
            });
        }

        [HttpGet("payment-info/{dealId}")]
        public async Task<IActionResult> GetPaymentInfo(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    int dealId)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // التحقق من التفويض
            if (deal.InvestorId != userId && deal.AuthorId != userId)
                return Unauthorized(new { Message = "You are not authorized to view this deal's payment information." });

            var response = new
            {
                DealId = deal.Id,
                BusinessName = deal.BusinessName,
                CurrentAmount = deal.OfferMoney,

                // معلومات التغيير
                ChangePayment = new
                {
                    Required = deal.IsChangePaymentRequired && !deal.IsChangePaymentProcessed,
                    AmountDifference = deal.ChangeAmountDifference,
                    PaymentDirection = deal.ChangeAmountDifference > 0 ? "investor_pays" : "refund_to_investor",
                    ChangeRequestId = deal.ChangeRequestId,
                    Processed = deal.IsChangePaymentProcessed,
                    ProcessedAt = deal.ChangePaymentProcessedAt?.ToString("yyyy-MM-dd HH:mm")
                },

                // معلومات العقد
                Contract = new
                {
                    CurrentVersion = deal.ContractVersion,
                    LastGenerated = deal.LastContractGeneratedAt?.ToString("yyyy-MM-dd HH:mm"),
                    CurrentUrl = deal.ContractDocumentUrl,
                    PreviousUrl = deal.PreviousContractDocumentUrl
                }
            };

            return Ok(response);
        }
        #endregion

        #region Helper Methods
        private async Task ProcessFinalProfitDistribution(int dealId)
        {
            // 1. Retrieve the complete deal details
            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null || deal.Status != DealStatus.Terminated)
                return;

            // 2. Calculate the deal duration
            var startDate = deal.CompletedAt ?? DateTime.UtcNow.AddMonths(-deal.DurationInMonths);
            var endDate = deal.ActualEndDate ?? DateTime.UtcNow;
            var totalInvestmentDays = (endDate - startDate).TotalDays;

            // 3. Get product revenue if this deal was converted to a product
            decimal finalRevenue = 0;
            DateTime? lastDistributionDate = null;

            if (deal.ProductId.HasValue)
            {
                var product = await _unitOfWork.Product.GetByIdAsync(deal.ProductId.Value);
                if (product != null)
                {
                    // Sum up all revenue from product sales since last profit distribution
                    var lastDistribution = await _unitOfWork.InvestmentProfit
                        .GetMostRecentProfitDistribution(dealId);

                    lastDistributionDate = lastDistribution?.DistributionDate ?? startDate;

                    // Calculate product revenue since last distribution
                    var orderItems = await _unitOfWork.OrderItem.GetByProductIdAndDateRange(
                        product.Id, lastDistributionDate.Value, endDate);

                    finalRevenue = orderItems.Sum(item => item.Price * item.Quantity);
                }
            }

            // 4. Check if there's any accumulated revenue to distribute
            if (finalRevenue > 0)
            {
                // 5. Calculate manufacturing and other costs
                decimal manufacturingCost = 0;
                if (deal.ProductId.HasValue && deal.ManufacturingCost > 0)
                {
                    var totalQuantitySold = await _unitOfWork.OrderItem.GetTotalQuantitySold(
                        deal.ProductId.Value, lastDistributionDate.Value, endDate);

                    manufacturingCost = deal.ManufacturingCost * totalQuantitySold;
                }

                decimal otherCosts = finalRevenue * 0.1m; // Estimate other costs as 10% of revenue

                // 6. Calculate net profit according to Mudarabah principles
                decimal netProfit = finalRevenue - manufacturingCost - otherCosts;

                if (netProfit > 0)
                {
                    // 7. Calculate profit shares based on the investment agreement
                    decimal investorShare = netProfit * (deal.OfferDeal / 100);
                    decimal platformFee = investorShare * (deal.PlatformFeePercentage / 100);
                    decimal ownerShare = netProfit - investorShare;

                    // 8. Create final profit distribution record
                    var finalDistribution = new DealProfit
                    {
                        DealId = deal.Id,
                        TotalRevenue = finalRevenue,
                        ManufacturingCost = manufacturingCost,
                        OtherCosts = otherCosts,
                        NetProfit = netProfit,
                        InvestorShare = investorShare - platformFee,
                        OwnerShare = ownerShare,
                        PlatformFee = platformFee,
                        DistributionDate = DateTime.UtcNow,
                        StartDate = startDate,
                        EndDate = endDate,
                        IsPaid = false,
                        IsPending = false,
                        IsApprovedByAdmin = true,
                        ApprovalDate = DateTime.UtcNow
                    };

                    await _unitOfWork.InvestmentProfit.AddAsync(finalDistribution);

                    // 9. Process the payment automatically
                    try
                    {
                        var transfer = await DistributeProfits(finalDistribution);

                        // Mark as paid
                        finalDistribution.IsPaid = true;
                        await _unitOfWork.InvestmentProfit.UpdateAsync(finalDistribution);

                        // Record the transaction
                        var transaction = new DealTransaction
                        {
                            DealId = deal.Id,
                            Amount = finalDistribution.InvestorShare,
                            Type = TransactionType.ProfitDistributionToInvestor,
                            TransactionId = transfer.Id,
                            Description = "Final profit distribution to investor upon termination"
                        };

                        await _unitOfWork.InvestmentTransaction.AddAsync(transaction);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with the process
                        _logger.LogError(ex, $"Error processing final profit payment for investment {deal.Id}");
                    }
                }
            }

            // 10. Handle capital return based on termination reason and agreement
            if (ShouldReturnCapital(deal))
            {
                decimal returnAmount = CalculateCapitalReturn(deal);

                if (returnAmount > 0)
                {
                    try
                    {
                        // Process refund through Stripe
                        var refund = await ProcessDealRefund(
                            deal, returnAmount, "Deal termination - capital return");

                        // Record the refund transaction
                        var transaction = new DealTransaction
                        {
                            DealId = deal.Id,
                            Amount = returnAmount,
                            Type = TransactionType.CapitalReturn,
                            TransactionId = refund.Id,
                            Description = "Return of capital upon deal termination"
                        };

                        await _unitOfWork.InvestmentTransaction.AddAsync(transaction);

                        // Create refund log
                        var refundLog = new PaymentRefundLog
                        {
                            OrderId = deal.Id,
                            RefundAmount = returnAmount,
                            RefundId = refund.Id,
                            RefundStatus = refund.Status,
                            RefundCreated = DateTime.UtcNow
                        };

                        await _unitOfWork.PaymentRefundLog.AddAsync(refundLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing capital return for deal {deal.Id}");
                    }
                }
            }

            // 11. Send notifications to both parties
            await SendFinalDistributionNotifications(deal);
        }

        private bool ShouldReturnCapital(Deal deal)
        {
            // Return capital based on termination reason and time elapsed
            switch (deal.EndReason)
            {
                case DealEndReason.Completed:
                    // No capital return for completed deals (full term)
                    return false;

                case DealEndReason.MutualAgreement:
                    // Return capital if mutually agreed
                    return true;

                case DealEndReason.OwnerTerminated:
                    // Always return capital if owner terminates early
                    return true;

                case DealEndReason.InvestorTerminated:
                    // Case-by-case for investor termination
                    var startDate = deal.CompletedAt ?? DateTime.UtcNow.AddMonths(-deal.DurationInMonths);
                    var endDate = deal.ActualEndDate ?? DateTime.UtcNow;
                    var elapsedMonths = (endDate - startDate).TotalDays / 30.0;

                    // Return capital if at least 75% of the term has elapsed
                    return elapsedMonths >= (deal.DurationInMonths * 0.75);

                case DealEndReason.AdminTerminated:
                case DealEndReason.Breach:
                default:
                    // Case-by-case based on termination notes
                    return deal.TerminationNotes?.Contains("capital return", StringComparison.OrdinalIgnoreCase) ?? false;
            }
        }

        private decimal CalculateCapitalReturn(Deal deal)
        {
            // Calculate how much capital to return based on duration and reason
            var startDate = deal.CompletedAt ?? DateTime.UtcNow.AddMonths(-deal.DurationInMonths);
            var endDate = deal.ActualEndDate ?? DateTime.UtcNow;
            var totalPossibleDays = deal.DurationInMonths * 30.0; // Approximate
            var actualDays = (endDate - startDate).TotalDays;
            var remainingPercentage = Math.Max(0, (totalPossibleDays - actualDays) / totalPossibleDays);

            switch (deal.EndReason)
            {
                case DealEndReason.Completed:
                    return 0; // No capital return

                case DealEndReason.MutualAgreement:
                    // Full remaining percentage
                    return Math.Round(deal.OfferMoney * (decimal)remainingPercentage, 2);

                case DealEndReason.OwnerTerminated:
                    // Higher return percentage if owner terminated
                    return Math.Round(deal.OfferMoney * (decimal)(remainingPercentage * 1.1), 2);

                case DealEndReason.InvestorTerminated:
                    // Lower return percentage if investor terminated
                    return Math.Round(deal.OfferMoney * (decimal)(remainingPercentage * 0.9), 2);

                case DealEndReason.AdminTerminated:
                    // Standard return
                    return Math.Round(deal.OfferMoney * (decimal)remainingPercentage, 2);

                case DealEndReason.Breach:
                    // No return for breach of contract by default
                    return 0;

                default:
                    // Default partial return
                    return Math.Round(deal.OfferMoney * (decimal)(remainingPercentage * 0.5), 2);
            }
        }

        private async Task SendFinalDistributionNotifications(Deal deal)
        {
            try
            {
                // Calculate total amounts for the notification
                var totalProfitsDistributed = await _unitOfWork.InvestmentProfit
                    .GetTotalProfitForDeal(deal.Id);

                var totalInvestorProfit = await _unitOfWork.InvestmentProfit
                    .GetTotalProfitForInvestor(deal.InvestorId!, deal.Id);

                var totalOwnerProfit = await _unitOfWork.InvestmentProfit
                    .GetTotalProfitForOwner(deal.AuthorId, deal.Id);

                var capitalReturned = await _unitOfWork.InvestmentTransaction
                    .GetTotalAmountByType(deal.Id, TransactionType.CapitalReturn);

                // Create message for the investor
                var investorMessage = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.InvestorId!,
                    MessageText = $"Your deal in '{deal.BusinessName}' has been finalized. " +
                                $"Total profits distributed: {totalInvestorProfit:C}. " +
                                (capitalReturned > 0 ? $"Capital returned: {capitalReturned:C}. " : "") +
                                "Thank you for your deal.",
                    IsRead = false,
                    MessageType = MessageType.General
                };

                // Create message for the business owner
                var ownerMessage = new DealMessage
                {
                    DealId = deal.Id,
                    SenderId = _adminUserId,
                    RecipientId = deal.AuthorId,
                    MessageText = $"The deal in '{deal.BusinessName}' has been finalized. " +
                                $"Total profits retained: {totalOwnerProfit:C}. " +
                                "Thank you for using our platform.",
                    IsRead = false,
                    MessageType = MessageType.General
                };

                await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
                await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending final distribution notifications for deal {deal.Id}");
            }
        }

        private async Task TerminateDeal(Deal deal, DealEndReason endReason, string notes, string terminatedById)
        {
            // Set termination details
            deal.Status = DealStatus.Terminated;
            deal.ActualEndDate = DateTime.UtcNow;
            deal.EndReason = endReason;
            deal.TerminationNotes = notes ?? "";

            // Process refund if applicable
            if (deal.IsPaymentProcessed && !string.IsNullOrEmpty(deal.PaymentIntentId))
            {
                try
                {
                    // Calculate refund amount based on termination reason and deal duration
                    decimal refundAmount = CalculateRefundAmount(deal);

                    if (refundAmount > 0)
                    {
                        var refund = await ProcessDealRefund(
                            deal, refundAmount, endReason.ToString());

                        // Record the refund transaction
                        var transaction = new DealTransaction
                        {
                            DealId = deal.Id,
                            Amount = refundAmount,
                            Type = TransactionType.Refund,
                            TransactionId = refund.Id,
                            Description = $"Deal refund due to termination: {endReason}"
                        };

                        await _unitOfWork.InvestmentTransaction.AddAsync(transaction);

                        // Create refund log
                        var refundLog = new PaymentRefundLog
                        {
                            OrderId = deal.Id, // Using investment ID since we don't have an order ID
                            RefundAmount = refundAmount,
                            RefundId = refund.Id,
                            RefundStatus = refund.Status,
                            RefundCreated = DateTime.UtcNow
                        };

                        await _unitOfWork.PaymentRefundLog.AddAsync(refundLog);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing refund for terminated deal {deal.Id}");

                    // Continue with termination even if refund fails
                }
            }

            // Process any final profit distributions
            await ProcessFinalProfitDistribution(deal.Id);
        }

        private decimal CalculateRefundAmount(Deal deal)
        {
            // Only refund if termination is early
            if (!deal.ScheduledEndDate.HasValue ||
                deal.ScheduledEndDate.Value <= DateTime.UtcNow)
            {
                return 0; // No refund for completed investments
            }

            // Calculate time elapsed as a percentage of total duration
            double totalDurationDays = (deal.ScheduledEndDate.Value - deal.CompletedAt.Value).TotalDays;
            double elapsedDays = (DateTime.UtcNow - deal.CompletedAt.Value).TotalDays;
            double remainingPercentage = Math.Max(0, (totalDurationDays - elapsedDays) / totalDurationDays);

            // Apply refund policy based on reason
            switch (deal.EndReason)
            {
                case DealEndReason.MutualAgreement:
                    // Full refund of remaining percentage
                    return Math.Round(deal.OfferMoney * (decimal)remainingPercentage, 2);

                case DealEndReason.OwnerTerminated:
                    // Higher refund percentage if owner terminated
                    return Math.Round(deal.OfferMoney * (decimal)(remainingPercentage * 1.1), 2);

                case DealEndReason.InvestorTerminated:
                    // Lower refund percentage if investor terminated
                    return Math.Round(deal.OfferMoney * (decimal)(remainingPercentage * 0.9), 2);

                case DealEndReason.AdminTerminated:
                    // Standard refund
                    return Math.Round(deal.OfferMoney * (decimal)remainingPercentage, 2);

                case DealEndReason.Breach:
                    // No refund for breach of contract
                    return 0;

                default:
                    // Default partial refund
                    return Math.Round(deal.OfferMoney * (decimal)(remainingPercentage * 0.5), 2);
            }
        }

        private async Task<Refund> ProcessDealRefund(Deal deal, decimal amount, string reason)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = deal.PaymentIntentId,
                    Amount = (long)(amount * 100), // Convert to cents
                    Reason = "requested",
                    Metadata = new Dictionary<string, string>
                    {
                        { "DealId", deal.Id.ToString() },
                        { "Reason", reason },
                        { "RefundType", "InvestmentTermination" }
                    }
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                return refund;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error processing refund for deal {deal.Id}");
                throw new ApplicationException($"Error processing refund: {ex.Message}");
            }
        }

        private async Task<Transfer> DistributeProfits(DealProfit profitDistribution)
        {
            try
            {
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

                var paymentService = new PaymentIntentService();
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

                var transferService = new TransferService();
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

        private async Task NotifyPartiesOfApproval(Deal deal, string adminId)
        {
            var investorMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = adminId,
                RecipientId = deal.InvestorId!,
                MessageText = $"Good news! Your deal in {deal.BusinessName} project has been approved by admin. Please review the contract and complete the process.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            var ownerMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = adminId,
                RecipientId = deal.AuthorId,
                MessageText = $"Good news! The deal from {deal.Investor!.FirstName} {deal.Investor.LastName} in your {deal.BusinessName} project has been approved. Please review the contract.",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
            await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
            await _unitOfWork.Complete();
        }

        private async Task NotifyPartiesOfRejection(Deal deal, string? reason, string adminId)
        {
            var investorMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = adminId,
                RecipientId = deal.InvestorId!,
                MessageText = $"Your deal in {deal.BusinessName} project has been rejected by admin. {reason}",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            var ownerMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = adminId,
                RecipientId = deal.AuthorId,
                MessageText = $"The deal from {deal.Investor!.FirstName} {deal.Investor.LastName} in your {deal.BusinessName} project has been rejected by admin. {reason}",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
            await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
            await _unitOfWork.Complete();
        }

        private async Task<DealProfitCalculationResult> CalculateMonthlyProfit(int dealId, DateTime startDate, DateTime endDate, decimal otherCosts = 0)
        {
            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null || deal.ProductId == null)
                throw new ArgumentException("Deal not found or not linked to a product");

            var result = new DealProfitCalculationResult();

            // Get all order items for this product in date range
            var orderItems = await _unitOfWork.OrderItem.GetByProductIdAndDateRange(
                deal.ProductId.Value, startDate, endDate);

            // Calculate revenue
            result.TotalQuantitySold = orderItems.Sum(item => item.Quantity);
            result.TotalRevenue = orderItems.Sum(item => item.Price * item.Quantity);

            // Calculate manufacturing cost
            result.ManufacturingCost = deal.ManufacturingCost * result.TotalQuantitySold;

            // Calculate net profit
            result.NetProfit = result.TotalRevenue - result.ManufacturingCost - otherCosts;

            // Split profit based on equity percentage
            result.PlatformFee = result.NetProfit * (deal.PlatformFeePercentage / 100);
            result.InvestorShare = (result.NetProfit - result.PlatformFee) * (deal.OfferDeal / 100);
            result.OwnerShare = result.NetProfit - result.PlatformFee - result.InvestorShare;

            // Calculate period information
            result.Period = $"{startDate:MMM yyyy}";

            return result;
        }

        // Helper method لتطبيق التغييرات مباشرة بدون دفع
        private async Task ApplyChangesDirectly(Deal deal, DealChangeRequest changeRequest)
        {
            var requestedValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                changeRequest.RequestedValues, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // تطبيق التغييرات
            if (requestedValues.TryGetValue("BusinessName", out var businessNameObj) && businessNameObj is System.Text.Json.JsonElement businessNameElement)
                deal.BusinessName = businessNameElement.GetString();

            if (requestedValues.TryGetValue("Description", out var descriptionObj) && descriptionObj is System.Text.Json.JsonElement descriptionElement)
                deal.Description = descriptionElement.GetString();

            if (requestedValues.TryGetValue("OfferDeal", out var equityObj) && equityObj is System.Text.Json.JsonElement equityElement)
                deal.OfferDeal = equityElement.GetDecimal();

            if (requestedValues.TryGetValue("DurationInMonths", out var durationObj) && durationObj is System.Text.Json.JsonElement durationElement)
                deal.DurationInMonths = durationElement.GetInt32();

            if (requestedValues.TryGetValue("ManufacturingCost", out var costObj) && costObj is System.Text.Json.JsonElement costElement)
                deal.ManufacturingCost = costElement.GetDecimal();

            // إنشاء عقد جديد
            await GenerateNewContract(deal, "Amendment");

            // تحديث حالة الطلب
            deal.IsChangePaymentProcessed = true;
            deal.ChangePaymentProcessedAt = DateTime.UtcNow;
            deal.ChangeRequestId = changeRequest.Id;

            await _unitOfWork.Deal.UpdateAsync(deal);
            await _unitOfWork.Complete();

            // إرسال إشعارات
            await SendChangeCompletedNotifications(deal);
        }

        // Helper method لإنشاء عقد جديد
        private async Task GenerateNewContract(Deal deal, string contractType)
        {
            // حفظ العقد السابق
            if (!string.IsNullOrEmpty(deal.ContractDocumentUrl))
            {
                deal.PreviousContractDocumentUrl = deal.ContractDocumentUrl;
            }

            // زيادة رقم إصدار العقد
            deal.ContractVersion += 1;
            deal.LastContractGeneratedAt = DateTime.UtcNow;

            // إنشاء العقد الجديد (يتطلب تطبيق method من PaymentController)
            // سيتم استدعاؤها من PaymentController
        }

        // Helper method لاستخراج القيم العشرية من JSON
        private decimal? GetDecimalFromJson(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value is System.Text.Json.JsonElement element)
            {
                if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;
            }
            return null;
        }

        // Helper method لإرسال إشعارات اكتمال التغيير
        private async Task SendChangeCompletedNotifications(Deal deal)
        {
            var investorMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId,
                RecipientId = deal.InvestorId!,
                MessageText = $"Changes to deal '{deal.BusinessName}' have been successfully applied. New contract version {deal.ContractVersion} is available.",
                IsRead = false,
                MessageType = MessageType.General
            };

            var ownerMessage = new DealMessage
            {
                DealId = deal.Id,
                SenderId = _adminUserId,
                RecipientId = deal.AuthorId,
                MessageText = $"Changes to deal '{deal.BusinessName}' have been successfully applied. New contract version {deal.ContractVersion} is available.",
                IsRead = false,
                MessageType = MessageType.General
            };

            await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
            await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
        }

        private async Task<List<string>> GetAllAdminUserIdsAsync()
        {
            try
            {
                var adminUsers = await _unitOfWork.Auth.GetUsersByRole("Admin");
                return adminUsers.Select(u => u.Id).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin users");
                // fallback to original admin if there's an error
                return new List<string> { _adminUserId };
            }
        }

        private async Task SendNotificationToAllAdminsAsync(int dealId, string senderId, string message, MessageType messageType = MessageType.AdminApprovalRequired)
        {
            try
            {
                var adminIds = await GetAllAdminUserIdsAsync();

                foreach (var adminId in adminIds)
                {
                    var notification = new DealMessage
                    {
                        DealId = dealId,
                        SenderId = senderId, // أو معرف المستخدم الحالي
                        RecipientId = adminId,
                        MessageText = message,
                        IsRead = false,
                        MessageType = messageType,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.InvestmentMessage.AddAsync(notification);
                }

                await _unitOfWork.Complete();
                _logger.LogInformation($"Notification sent to {adminIds.Count} admins for deal {dealId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notifications to admins for deal {dealId}");
                throw;
            }
        }

        #endregion
    }
}