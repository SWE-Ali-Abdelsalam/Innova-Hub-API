using InnoHub.ModelDTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InnoHub.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using InnoHub.Service.EmailSenderService;
using Ecommerce_platforms.Repository.Auth;
using InnoHub.UnitOfWork;
using System;
using InnoHub.Service;
namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Add RoleManager
        private readonly IEmailSender _emailSender;
        private readonly IAuth _auth;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProfileController> _logger;
        private readonly OtpService _otpService;

        public ProfileController(IAuth auth, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender, IUnitOfWork unitOfWork, ILogger<ProfileController> logger, OtpService otpService)
        {
            _auth = auth;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _otpService = otpService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Extract user ID from the token
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            // Find the user by their ID
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            // Retrieve the roles for the user
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault(); // Assuming the user has only one role
            
            // Construct the full URL for the profile images
            string profileImageUrl = null;
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                profileImageUrl = $"https://innova-hub.premiumasp.net/{user.ProfileImageUrl}";

            string profileCoverUrl = null;
            if (!string.IsNullOrEmpty(user.ProfileCoverUrl))
                profileCoverUrl = $"https://innova-hub.premiumasp.net/{user.ProfileCoverUrl}";

            // Return the user profile data
            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.City,
                user.PhoneNumber,
                user.District,
                user.Country,
                ProfileImageUrl = profileImageUrl,
                ProfileCoverUrl = profileCoverUrl,
                RoleName = roleName,
                IsVerified = user.IsIdCardVerified,
                TotalBalance = user.TotalAccountBalance,
            });
        }

        [HttpGet("profile-by-id/{id}")]
        public async Task<IActionResult> GetProfileById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Unauthorized(new { Message = "User not authenticated." });

            // Find the user by their ID
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            // Retrieve the roles for the user
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault(); // Assuming the user has only one role

            // Retrieve RoleId using RoleManager if the role name is found
            string roleId = null;
            if (!string.IsNullOrEmpty(roleName))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                roleId = role?.Id;
            }
            // Construct the full URL for the profile images
            string profileImageUrl = null;
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                profileImageUrl = $"https://innova-hub.premiumasp.net/{user.ProfileImageUrl}";

            string profileCoverUrl = null;
            if (!string.IsNullOrEmpty(user.ProfileCoverUrl))
                profileCoverUrl = $"https://innova-hub.premiumasp.net/{user.ProfileCoverUrl}";

            // Return the user profile data
            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.City,
                user.PhoneNumber,
                user.District,
                user.Country,
                ProfileImageUrl = profileImageUrl,
                ProfileCoverUrl = profileCoverUrl,
                RoleName = roleName,
                RoleId = roleId,
                TotalBalance = user.TotalAccountBalance,
            });
        }

        // Update Profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] UpdateProfileDTO updateProfileDTO)
        {
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            user.FirstName = updateProfileDTO.FirstName ?? user.FirstName;
            user.LastName = updateProfileDTO.LastName ?? user.LastName;
            user.City = updateProfileDTO.City ?? user.City;
            user.PhoneNumber = updateProfileDTO.PhoneNumber ?? user.PhoneNumber;
            user.District = updateProfileDTO.District ?? user.District;
            user.Country = updateProfileDTO.Country ?? user.Country;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return Ok(new { Message = "Profile updated successfully." });
        }

        // Change Profile Picture
        [HttpPost("profile-picture")]
        public async Task<IActionResult> ChangeProfilePicture([FromHeader(Name = "Authorization")] string authorizationHeader, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            // Validate file types
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { Message = "Only .jpg, .jpeg, or .png files are allowed." });

            // Validate file sizes (10MB max)
            const int maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSizeInBytes)
                return BadRequest(new { Message = "Maximum file size allowed is 10MB." });

            string profileImagePath = null;

            try
            {
                var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/ProfileImages");

                profileImagePath = await _unitOfWork.FileService.SaveFileAsync(file, folderPath);

                user.ProfileImageUrl = profileImagePath;

                await _unitOfWork.Auth.UpdateUser(user);

                return Ok(new { Message = "Profile picture uploaded successfully.", ProfileImageUrl = user.ProfileImageUrl });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading profile picture for user {userId}");
                return StatusCode(500, new { Message = "An error occurred while uploading profile picture." });
            }
        }

        [HttpPost("profile-cover")]
        public async Task<IActionResult> ChangeProfileCover([FromHeader(Name = "Authorization")] string authorizationHeader, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            // Validate file types
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { Message = "Only .jpg, .jpeg, or .png files are allowed." });

            // Validate file sizes (10MB max)
            const int maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSizeInBytes)
                return BadRequest(new { Message = "Maximum file size allowed is 10MB." });

            string profileCoverPath = null;

            try
            {
                var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/ProfileImages");

                profileCoverPath = await _unitOfWork.FileService.SaveFileAsync(file, folderPath);

                user.ProfileCoverUrl = profileCoverPath;

                await _unitOfWork.Auth.UpdateUser(user);

                return Ok(new { Message = "Cover picture uploaded successfully.", ProfileCoverUrl = user.ProfileCoverUrl });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading cover picture for user {userId}");
                return StatusCode(500, new { Message = "An error occurred while uploading cover picture." });
            }
        }

        // Delete Profile Picture
        [HttpDelete("profile-picture")]
        public async Task<IActionResult> DeleteProfilePicture([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            if (string.IsNullOrWhiteSpace(user.ProfileImageUrl))
                return NotFound(new { Message = "No profile picture found." });

            var filePath = Path.Combine("wwwroot", user.ProfileImageUrl);
            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error deleting profile picture {filePath} for user {userId}");
            }

            user.ProfileImageUrl = "/ProfileImages/DefaultImage.png";
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return Ok(new { Message = "Profile picture deleted successfully." });
        }
        [HttpDelete("profile-cover")]
        public async Task<IActionResult> DeleteProfileCover([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            if (string.IsNullOrWhiteSpace(user.ProfileCoverUrl))
                return NotFound(new { Message = "No profile cover found." });

            var filePath = Path.Combine("wwwroot", user.ProfileImageUrl);
            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting profile picture {filePath} for user {userId}");
            }

            user.ProfileCoverUrl = "/ProfileImages/DefaultCover.jpg";
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return Ok(new { Message = "Profile cover deleted successfully." });
        }

        [HttpPost("upload-id-card")]
        public async Task<IActionResult> UploadIdCard(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromForm] UploadIdCardDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var user = await _unitOfWork.Auth.GetUserById(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "BusinessOwner");
            if (!await _unitOfWork.Auth.IsInvestor(userId) && currentUser == null)
                return Unauthorized(new { Message = "Only investors or business owners can can upload their ID Cards." });

            // Validate files
            if (request.FrontImage == null || request.BackImage == null)
                return BadRequest(new { Message = "Both front and back images of the ID card are required." });

            // Validate file types
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var frontExtension = Path.GetExtension(request.FrontImage.FileName).ToLowerInvariant();
            var backExtension = Path.GetExtension(request.BackImage.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(frontExtension) || !allowedExtensions.Contains(backExtension))
                return BadRequest(new { Message = "Only .jpg, .jpeg, or .png files are allowed." });

            // Validate file sizes (10MB max)
            const int maxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
            if (request.FrontImage.Length > maxFileSizeInBytes || request.BackImage.Length > maxFileSizeInBytes)
                return BadRequest(new { Message = "Maximum file size allowed is 10MB." });

            string frontCardPicturePath = null;
            string backCardPicturePath = null;

            try
            {
                // Delete old signature if exists
                if (!string.IsNullOrEmpty(user.IdCardFrontImageUrl))
                    _unitOfWork.FileService.DeleteFile(user.IdCardFrontImageUrl);

                if (!string.IsNullOrEmpty(user.IdCardBackImageUrl))
                    _unitOfWork.FileService.DeleteFile(user.IdCardBackImageUrl);

                var folderPath = _unitOfWork.FileService.EnsureDirectory("wwwroot/IdentityImages");

                frontCardPicturePath = await _unitOfWork.FileService.SaveFileAsync(request.FrontImage, folderPath);
                backCardPicturePath = await _unitOfWork.FileService.SaveFileAsync(request.BackImage, folderPath);

                // Update user record
                user.IdCardFrontImageUrl = frontCardPicturePath;
                user.IdCardBackImageUrl = backCardPicturePath;
                user.IdCardUploadDate = DateTime.UtcNow;
                user.IsIdCardVerified = false; // Reset verification status if already verified
                user.IdCardVerificationDate = null;
                user.IdCardVerifiedByUserId = null;
                user.IdCardRejectionReason = null;

                await _unitOfWork.Auth.UpdateUser(user);

                // Notify admins about new ID card uploads
                //await NotifyAdminAboutNewIdCard(userId, user.Email);

                return Ok(new
                {
                    Message = "ID card images uploaded successfully. Verification is pending.",
                    FrontImageUrl = $"https://innova-hub.premiumasp.net{user.IdCardFrontImageUrl}",
                    BackImageUrl = $"https://innova-hub.premiumasp.net{user.IdCardBackImageUrl}",
                    UploadDate = user.IdCardUploadDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Uploaded"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading ID card images for user {userId}");
                return StatusCode(500, new { Message = "An error occurred while uploading ID card images." });
            }
        }

        // Change Password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromHeader(Name = "Authorization")] string authorizationHeader, [FromBody] UpdatePasswordDTO updatePasswordDTO)
        {
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            var result = await _userManager.ChangePasswordAsync(user, updatePasswordDTO.CurrentPassword, updatePasswordDTO.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return Ok(new { Message = "Password changed successfully." });
        }

        [HttpPost("generate-token")]
        public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenForResetPasswordDTO generateTokenForResetPasswordDTO)
        {
            if (string.IsNullOrEmpty(generateTokenForResetPasswordDTO.Email))
            {
                return BadRequest(new { Message = "Email is required." });
            }

            var user = await _userManager.FindByEmailAsync(generateTokenForResetPasswordDTO.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            // Generate the actual reset token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Generate a 6-digit OTP
            var otp = _otpService.GenerateOtp();

            // Store the OTP and its associated token (no expiry)
            _otpService.StoreOtp(generateTokenForResetPasswordDTO.Email, otp, resetToken);

            // Send the OTP to the user's email
            await _emailSender.SendEmailAsync(
                generateTokenForResetPasswordDTO.Email,
                "Password Reset Verification Code",
                $"Your verification code is: {otp}\n\nPlease use this code to reset your password."
            );

            return Ok(new { Message = "A verification code has been sent to your email." });
        }

        // Reset password using the token and new password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithOtpDTO resetPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Invalid input.", Details = ModelState.Values.SelectMany(v => v.Errors) });

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            // Retrieve the actual token using the OTP
            var token = _otpService.GetTokenByOtp(resetPasswordDto.Email, resetPasswordDto.Otp);
            if (string.IsNullOrEmpty(token))
                return BadRequest(new { Message = "Invalid or expired verification code." });

            // Reset the password using the retrieved token
            var result = await _userManager.ResetPasswordAsync(user, token, resetPasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Message = "Password reset failed.",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { Message = "Password has been reset successfully." });
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromBody] DeleteAccountDTO deleteAccountDTO)
        {
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            // إذا كان UserId غير موجود في DTO، نعتبر أنه المستخدم الحالي هو من يحذف حسابه
            if (string.IsNullOrEmpty(deleteAccountDTO.UserId))
            {
                // تأكد من إرسال كلمة المرور إذا كان المستخدم هو من يحذف حسابه
                if (string.IsNullOrEmpty(deleteAccountDTO.Password))
                    return BadRequest(new { Message = "Password is required to delete your own account." });

                // التحقق من كلمة السر للمستخدم
                if (!await _userManager.CheckPasswordAsync(user, deleteAccountDTO.Password))
                    return Unauthorized(new { Message = "Invalid password." });

                // حذف الحساب
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

                return Ok(new { Message = "User account deleted successfully." });
            }

            // إذا كان UserId موجود، نعتبر أنه Admin يحاول حذف حساب مستخدم آخر
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");

            if (currentUser == null)
                return Unauthorized(new { Message = "Only admins can delete other users' accounts." });

            var accountToDelete = await _userManager.FindByIdAsync(deleteAccountDTO.UserId);
            if (accountToDelete == null)
                return NotFound(new { Message = "Account to delete not found." });

            var deleteResult = await _userManager.DeleteAsync(accountToDelete);
            if (!deleteResult.Succeeded)
                return BadRequest(new { Errors = deleteResult.Errors.Select(e => e.Description) });

            //var roleName = await _unitOfWork.Auth.GetRoleNameAsync(currentUser);
            //if (roleName == "BusinessOwner")
            //{
            //    _unitOfWork.DbContext.Remove(new Owner { Id = userId });
            //    await _unitOfWork.Complete();
            //}

            return Ok(new { Message = "User account deleted successfully." });
        }

        [HttpGet("getOrders")]
        public async Task<IActionResult> GetOrders(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    int page = 1,
    int pageSize = 5)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var allOrders = await _unitOfWork.Order.GetAllOrdersForSpecificUser(userId);

            var totalOrders = allOrders.Count();
            var pagedOrders = allOrders
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var orderList = pagedOrders.Select(o => new
            {
                orderId = o.Id,
                orderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                totalAmount = $"EGP {o.Total:N2}",
                items = o.OrderItems.Select(i => new
                {
                    ProductId = i.Product.Id,
                    productName = i.Product.Name,
                    ProductDescription = i.Product.Description,
                    price = $"${i.Price:N2}",
                    imageUrl = $"https://innova-hub.premiumasp.net{i.Product.HomePicture}",

                })
            });

            return Ok(new
            {
                totalOrders,
                pageNumber = page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize),
                orders = orderList
            });
        }

    }
}
