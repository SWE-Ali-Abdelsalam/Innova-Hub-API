using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Service.FileService;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignatureController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SignatureController> _logger;
        private readonly IFileService _fileService;

        public SignatureController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment environment,
            ILogger<SignatureController> logger,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
            _logger = logger;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMySignature(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var user = await _unitOfWork.Auth.GetUserById(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            if (string.IsNullOrEmpty(user.SignatureImageUrl))
                return Ok(new { HasSignature = false });

            return Ok(new
            {
                HasSignature = true,
                SignatureUrl = user.SignatureImageUrl,
                UploadDate = user.SignatureUploadDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Uploaded"
            });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadSignature(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromForm] UploadSignatureDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var user = await _unitOfWork.Auth.GetUserById(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
            if (!await _unitOfWork.Auth.IsInvestor(userId) && currentUser == null)
                return Unauthorized(new { Message = "Only investors or business owners can can upload their signatures." });

            // Validate files
            if (request.SignatureImage == null)
                return BadRequest(new { Message = "Signature image is required." });

            // Validate file types
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", "svg" };
            var fileExtension = Path.GetExtension(request.SignatureImage.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { Message = "Only .jpg, .jpeg, .png, or svg files are allowed." });

            // Validate file sizes (10MB max)
            const int maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
            if (request.SignatureImage.Length > maxFileSizeInBytes)
                return BadRequest(new { Message = "Maximum file size allowed is 10MB." });

            string signaturePicturePath = null;

            try
            {
                // Delete old signature if exists
                if (!string.IsNullOrEmpty(user.SignatureImageUrl))
                    _unitOfWork.FileService.DeleteFile(user.SignatureImageUrl);

                var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/SignatureImages");

                signaturePicturePath = await _unitOfWork.FileService.SaveFileAsync(request.SignatureImage, folderPath);

                // Update user record
                user.SignatureImageUrl = signaturePicturePath;
                user.SignatureUploadDate = DateTime.UtcNow;
                user.IsSignatureVerified = false; // Reset verification status if already verified
                user.SignatureVerificationDate = null;
                user.SignatureVerifiedByUserId = null;
                user.SignatureRejectionReason = null;

                await _unitOfWork.Auth.UpdateUser(user);

                // Notify admins about new signature upload
                //await NotifyAdminAboutNewIdCard(userId, user.Email);

                return Ok(new
                {
                    Message = "Signature image uploaded successfully. Verification is pending.",
                    SignatureImageUrl = $"https://innova-hub.premiumasp.net{user.SignatureImageUrl}",
                    UploadDate = user.SignatureUploadDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Uploaded"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading signature image for user {userId}");
                return StatusCode(500, new { Message = "An error occurred while uploading signature image." });
            }
        }

        [HttpDelete("delete-signature")]
        public async Task<IActionResult> DeleteSignature(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var user = await _unitOfWork.Auth.GetUserById(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            if (string.IsNullOrEmpty(user.SignatureImageUrl))
                return BadRequest(new { Message = "No signature found to delete." });

            try
            {
                // Delete signature file
                _fileService.DeleteFile(user.SignatureImageUrl);

                // تحديث بيانات المستخدم
                user.SignatureImageUrl = null;
                user.SignatureUploadDate = null;

                await _unitOfWork.Auth.UpdateUser(user);

                return Ok(new { Message = "Signature deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting signature for user {UserId}", userId);
                return StatusCode(500, new { Message = "An error occurred while deleting the signature." });
            }
        }

        [HttpGet("contract/{dealId}")]
        public async Task<IActionResult> GetContract(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            int dealId)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null)
                return NotFound(new { Message = "Deal not found." });

            // Check if the user is authorized to view this contract
            bool isOwner = deal.AuthorId == userId;
            bool isInvestor = deal.InvestorId == userId;
            bool isAdmin = await _unitOfWork.Auth.IsAdmin(userId);

            if (!isOwner && !isInvestor && !isAdmin)
                return Unauthorized(new { Message = "You are not authorized to view this contract." });

            if (string.IsNullOrEmpty(deal.ContractDocumentUrl))
                return NotFound(new { Message = "No contract has been generated for this deal yet." });

            return Ok(new
            {
                ContractUrl = deal.ContractDocumentUrl,
                OwnerSignedAt = deal.OwnerSignedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Signed",
                InvestorSignedAt = deal.InvestorSignedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Signed"
            });
        }
    }
}