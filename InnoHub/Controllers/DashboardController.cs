using AutoMapper;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.MLService;
using InnoHub.ModelDTO;
using InnoHub.ModelDTO.ML;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DashboardController> _logger;
        private readonly MLSpamDetectionService _mlSpamDetectionService;

        public DashboardController(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<DashboardController> logger, MLSpamDetectionService mlSpamDetectionService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _mlSpamDetectionService = mlSpamDetectionService;

        }

        [HttpGet("GetAllDeals")]
        public async Task<IActionResult> GetAllDeals(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromQuery] bool isApproved,
    [FromQuery] string? dealName = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can see these deals." });
            }

            // أولاً استخدم await لاسترجاع البيانات من GetDealsByApprovalAsync
            var allDeals = await _unitOfWork.Deal.GetDealsByApprovalAsync(page, pageSize, isApproved);  // تأكد من تمرير الـ pageSize هنا

            // Apply optional name filter
            if (!string.IsNullOrEmpty(dealName))
            {
                allDeals = allDeals
                    .Where(d => d.BusinessName != null && d.BusinessName.Contains(dealName, StringComparison.OrdinalIgnoreCase))
                    .ToList();  // .ToList() لتصفية البيانات في الذاكرة بعد استرجاعها
            }

            // احسب إجمالي عدد الـ Deals بعد التصفية
            int totalCount = allDeals.Count();  // الآن تستطيع استخدام Count على allDeals لأنه أصبح IEnumerable<Deal>

            // احسب عدد الصفحات بناءً على العدد الإجمالي
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // تطبيق الـ pagination على الـ Deals
            var pagedDeals = allDeals
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();  // هذه هي الـ Deals المعروضة في الصفحة الحالية

            // تحويل الـ Deals لهيكل بيانات مناسب للـ response
            var response = pagedDeals.Select((deal, index) => new
            {
                Id = deal.Id,
                Index = (page - 1) * pageSize + index + 1,
                OwnerName = $"{deal.Author?.FirstName ?? "Unknown"} {deal.Author?.LastName ?? ""}".Trim(),
                DealName = deal.BusinessName,
                Description = deal.Description,
                OfferDeal = deal.OfferDeal,
                CategoryName = deal.Category?.Name ?? "Uncategorized",
                OfferMoney = deal.OfferMoney,
                ApprovedAt = deal.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not Approved"
            }).ToList();

            // رجع الـ Deals مع الـ pagination metadata
            return Ok(new
            {
                Metadata = new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                },
                Deals = response
            });
        }

        [HttpPost("PublishDeal/{id}")]
        public async Task<IActionResult> PublishDeal([FromHeader(Name = "Authorization")] string authorizationHeader, int id)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can update categories." });
            }

            // Retrieve the Deal from the database using the DealId
            var Deal = await _unitOfWork.Deal.GetByIdAsync(id);

            // If the Deal is not found, return a BadRequest response
            if (Deal == null)
            {
                return NotFound(new { Message = "Deal not found." });
            }

            // If the Deal is already approved, return a message indicating so
            if (Deal.IsApproved)
            {
                return BadRequest(new { Message = "Deal is already approved." });
            }

            // Approve the Deal
            Deal.IsApproved = true;
            Deal.ApprovedAt = DateTime.UtcNow;

            // Save the changes to the database
            await _unitOfWork.Complete();

            // Return the updated product as part of the response (optional)
            return Ok(new
            {
                Message = "Deal approved successfully.",
                Deal = new
                {
                    Deal.Id,
                    Deal.BusinessName,
                    Deal.IsApproved
                }
            });
        }

        [HttpPost("verify-id-card")]
        public async Task<IActionResult> VerifyIdCard(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] VerifyIdCardDTO request)
        {
            var adminId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Verify that the user is an admin
            if (!await _unitOfWork.Auth.IsAdmin(adminId))
                return Unauthorized(new { Message = "Only admins can verify ID cards." });

            var user = await _unitOfWork.Auth.GetUserById(request.UserId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            if (string.IsNullOrEmpty(user.IdCardFrontImageUrl) || string.IsNullOrEmpty(user.IdCardBackImageUrl))
                return BadRequest(new { Message = "User has no uploaded ID card images." });

            if (request.IsApproved)
            {
                user.IsIdCardVerified = true;
                user.IdCardVerificationDate = DateTime.UtcNow;
                user.IdCardVerifiedByUserId = adminId;
                user.IdCardRejectionReason = request.RejectionReason;
            }
            else
            {
                try
                {
                    _unitOfWork.FileService.DeleteFile(user.IdCardFrontImageUrl);
                    _unitOfWork.FileService.DeleteFile(user.IdCardBackImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting ID card images for user {UserId}", user.Id);
                }
                user.IdCardFrontImageUrl = null;
                user.IdCardBackImageUrl = null;
                user.IsIdCardVerified = false;
                user.IdCardVerificationDate = DateTime.UtcNow;
                user.IdCardVerifiedByUserId = adminId;
                user.IdCardRejectionReason = request.RejectionReason;
            }

            await _unitOfWork.Auth.UpdateUser(user);
            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = request.IsApproved ? "ID card verified successfully." : "ID card verification rejected.",
                IsVerified = user.IsIdCardVerified,
                VerificationDate = user.IdCardVerificationDate?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown",
                RejectionReason = user.IdCardRejectionReason
            });
        }

        [HttpPost("verify-signature")]
        public async Task<IActionResult> VerifySignature(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] VerifyIdCardDTO request)
        {
            var adminId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Verify that the user is an admin
            if (!await _unitOfWork.Auth.IsAdmin(adminId))
                return Unauthorized(new { Message = "Only admins can signatures." });

            var user = await _unitOfWork.Auth.GetUserById(request.UserId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            if (string.IsNullOrEmpty(user.SignatureImageUrl))
                return BadRequest(new { Message = "User has no uploaded signature images." });

            if (request.IsApproved)
            {
                user.IsSignatureVerified = true;
                user.SignatureVerificationDate = DateTime.UtcNow;
                user.SignatureVerifiedByUserId = adminId;
                user.SignatureRejectionReason = request.RejectionReason;
            }
            else
            {
                try
                {
                    _unitOfWork.FileService.DeleteFile(user.SignatureImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting signature images for user {UserId}", user.Id);
                }
                user.SignatureImageUrl = null;
                user.IsSignatureVerified = false;
                user.SignatureVerificationDate = DateTime.UtcNow;
                user.SignatureVerifiedByUserId = adminId;
                user.SignatureRejectionReason = request.RejectionReason;
            }

            await _unitOfWork.Auth.UpdateUser(user);
            await _unitOfWork.Complete();

            return Ok(new
            {
                Message = request.IsApproved ? "Signature verified successfully." : "Signature verification rejected.",
                IsVerified = user.IsSignatureVerified,
                VerificationDate = user.SignatureVerificationDate?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown",
                RejectionReason = user.SignatureRejectionReason
            });
        }

        [HttpGet("getAllProducts")]
        public async Task<IActionResult> GetAllProducts(
     [FromHeader(Name = "Authorization")] string authorizationHeader,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? productName = null,
     [FromQuery] decimal? price = null,
     [FromQuery] string? authorName = null,
     [FromQuery] string? categoryName = null,
     [FromQuery] int? productId = null)  // Optional filter for ProductId
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can update categories." });
            }

            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { Message = "Page and pageSize must be greater than zero." });
            }

            // Build filter
            Expression<Func<Product, bool>> filter = product =>
                (string.IsNullOrEmpty(productName) || product.Name.ToLower().Contains(productName.ToLower())) &&
                (!price.HasValue || product.Price == price.Value) &&
                (string.IsNullOrEmpty(authorName) || (product.Author != null &&
                    (product.Author.FirstName + " " + product.Author.LastName).ToLower().Contains(authorName.ToLower()))) &&
                (string.IsNullOrEmpty(categoryName) || (product.Category != null &&
                    product.Category.Name.ToLower().Contains(categoryName.ToLower()))) &&
                (!productId.HasValue || product.Id == productId.Value);  // Apply ProductId filter if provided

            var totalProducts = await _unitOfWork.Product.CountAsync(filter);

            var metadata = new
            {
                TotalProducts = totalProducts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize)
            };

            if (totalProducts == 0)
            {
                return Ok(new
                {
                    Metadata = metadata,
                    Products = new List<object>()
                });
            }

            var products = await _unitOfWork.Product.GetPaginatedAsync(
                page,
                pageSize,
                orderBy: "Id",
                descending: true,
                includes: new List<Expression<Func<Product, object>>>
                {
            p => p.Author,
            p => p.ProductPictures,
            p => p.Sizes,
            p => p.Colors,
            p => p.Ratings,
            p => p.Category
                },
                filter: filter
            );

            // Map products to response with the index starting from 0
            var productResponses = products
                .Select((product, index) => new
                {
                    Index = index,  // Start index from 0
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductAuthorName = product.Author != null
                        ? $"{product.Author.FirstName} {product.Author.LastName}"
                        : "Unknown",
                    ProductDescription = product.Description,
                    Category = product.Category != null ? product.Category.Name : "Unknown",
                    ProductSizes = product.Sizes?.Select(s => s.SizeName).ToList() ?? new List<string>(),
                    ProductColors = product.Colors?.Select(c => c.ColorName).ToList() ?? new List<string>(),
                    Dimensions = product.Dimensions,
                    ProductPrice = product.Discount > 0
                        ? product.Price * (1 - product.Discount / 100)
                        : product.Price,
                    ProductStock = product.Stock
                })
                .ToList();

            return Ok(new
            {
                Metadata = metadata,
                Products = productResponses
            });
        }

        [HttpGet("verifications")]
        public async Task<IActionResult> GetPendingVerifications(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromQuery] bool? isVerified = null)
        {
            var adminId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            // Verify that the user is an admin
            if (!await _unitOfWork.Auth.IsAdmin(adminId))
                return Unauthorized(new { Message = "Only admins can view pending verifications." });

            var pendingUsers = await _unitOfWork.Auth.GetUsersWithPendingIdVerification();

            if (isVerified.HasValue)
            {
                pendingUsers = pendingUsers
                    .Where(u => u.IsIdCardVerified == isVerified.Value)
                    .ToList();
            }

            var pendingVerifications = pendingUsers.Select((user, index) => new
            {
                Index = index + 1,
                UserName = $"{user.FirstName} {user.LastName}",
                Role = _unitOfWork.Auth.GetRoleNameAsync(user).Result,
                UserId = user.Id,
                Email = user.Email,
                FrontIdUrl = $"https://innova-hub.premiumasp.net{user.IdCardFrontImageUrl}",
                BackIdUrl = $"https://innova-hub.premiumasp.net{user.IdCardBackImageUrl}",
                UploadDate = user.IdCardUploadDate?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown",
                VerificationDate = user.IdCardVerificationDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not Verified"
            }).ToList();

            return Ok(new
            {
                Count = pendingVerifications.Count,
                PendingVerifications = pendingVerifications
            });
        }


        [HttpGet("getAllReports")]
        public async Task<IActionResult> GetAllReports([FromQuery] string type, [FromQuery] string reporterName, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Log incoming parameters
                _logger.LogInformation($"Getting reports with type: {type}, reporterName: {reporterName}, page: {page}, pageSize: {pageSize}");

                var reports = await _unitOfWork.Report.GetAllReports();

                // Filter by type if provided
                if (!string.IsNullOrEmpty(type) && Enum.TryParse(type, true, out ReportedEntityType reportedType))
                {
                    reports = reports.Where(r => r.ReportedType == reportedType).ToList();
                }

                // Filter by reporter name if provided
                if (!string.IsNullOrEmpty(reporterName))
                {
                    var normalizedReporterName = reporterName.Trim().ToLower();
                    reports = reports.Where(report =>
                    {
                        var reporter = report.Reporter ?? _unitOfWork.AppUser.GetUSerByIdAsync(report.ReporterId).Result;
                        if (reporter == null) return false;

                        var fullName = $"{reporter.FirstName} {reporter.LastName}".ToLower();
                        return fullName.Contains(normalizedReporterName);
                    }).ToList();
                }

                // Apply pagination
                var totalReports = reports.Count;
                var totalPages = (int)Math.Ceiling((double)totalReports / pageSize);
                var paginatedReports = reports
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var reportDetails = new List<object>();

                // Process each report to include entity-specific details
                foreach (var (report, index) in paginatedReports.Select((value, idx) => (value, idx)))
                {
                    try
                    {
                        var reporter = report.Reporter;
                        if (reporter == null && !string.IsNullOrEmpty(report.ReporterId))
                        {
                            reporter = await _unitOfWork.AppUser.GetUSerByIdAsync(report.ReporterId);
                        }

                        var reporterDisplayName = "Unknown User";
                        if (reporter != null)
                        {
                            reporterDisplayName = $"{reporter.FirstName ?? ""} {reporter.LastName ?? ""}".Trim();
                            if (string.IsNullOrWhiteSpace(reporterDisplayName)) reporterDisplayName = "Unknown User";
                        }

                        // Default values
                        var reportDetail = new
                        {
                            ReportId = report.Id,
                            ReporterId = report.ReporterId,
                            ReporterName = reporterDisplayName,
                            ReportedType = report.ReportedType.ToString(),
                            Message = report.Message ?? "",
                            CreatedAt = report.CreatedAt,
                            Index = index + ((page - 1) * pageSize),
                            ReportedName = "Unknown",
                            ReportedId = report.ReportedId ?? "",
                            Images = new List<string>()
                        };

                        // Add type-specific information
                        switch (report.ReportedType)
                        {
                            case ReportedEntityType.Product:
                                if (int.TryParse(report.ReportedId, out int productId))
                                {
                                    var product = await _unitOfWork.Product.GetByIdAsync(productId);
                                    if (product != null)
                                    {
                                        var images = new List<string>();

                                        // Add home picture if available
                                        if (!string.IsNullOrEmpty(product.HomePicture))
                                        {
                                            images.Add($"https://innova-hub.premiumasp.net{product.HomePicture}");
                                        }

                                        // Add additional pictures if available
                                        if (product.ProductPictures != null)
                                        {
                                            images.AddRange(product.ProductPictures
                                                .Where(p => p != null && !string.IsNullOrEmpty(p.PictureUrl))
                                                .Select(p => $"https://innova-hub.premiumasp.net{p.PictureUrl}"));
                                        }

                                        reportDetail = new
                                        {
                                            ReportId = report.Id,
                                            ReporterId = report.ReporterId,
                                            ReporterName = reporterDisplayName,
                                            ReportedType = report.ReportedType.ToString(),
                                            Message = report.Message ?? "",
                                            CreatedAt = report.CreatedAt,
                                            Index = index + ((page - 1) * pageSize),
                                            ReportedName = product.Name ?? "Unnamed Product",
                                            ReportedId = productId.ToString(),
                                            Images = images
                                        };
                                    }
                                }
                                break;

                            case ReportedEntityType.Deal:
                                if (int.TryParse(report.ReportedId, out int dealId))
                                {
                                    var deal = await _unitOfWork.Deal.GetByIdAsync(dealId);
                                    if (deal != null)
                                    {
                                        var images = new List<string>();

                                        // Add deal pictures if available
                                        if (deal.Pictures != null)
                                        {
                                            images.AddRange(deal.Pictures
                                                .Where(p => !string.IsNullOrEmpty(p))
                                                .Select(p => $"https://innova-hub.premiumasp.net{p}"));
                                        }

                                        reportDetail = new
                                        {
                                            ReportId = report.Id,
                                            ReporterId = report.ReporterId,
                                            ReporterName = reporterDisplayName,
                                            ReportedType = report.ReportedType.ToString(),
                                            Message = report.Message ?? "",
                                            CreatedAt = report.CreatedAt,
                                            Index = index + ((page - 1) * pageSize),
                                            ReportedName = deal.BusinessName ?? "Unnamed Deal",
                                            ReportedId = dealId.ToString(),
                                            Images = images
                                        };
                                    }
                                }
                                break;

                            case ReportedEntityType.User:
                                if (!string.IsNullOrEmpty(report.ReportedId))
                                {
                                    var user = await _unitOfWork.AppUser.GetUSerByIdAsync(report.ReportedId);
                                    if (user != null)
                                    {
                                        var profileImage = !string.IsNullOrEmpty(user.ProfileImageUrl)
                                            ? $"https://innova-hub.premiumasp.net{user.ProfileImageUrl}"
                                            : null;

                                        var images = new List<string>();
                                        if (!string.IsNullOrEmpty(profileImage))
                                        {
                                            images.Add(profileImage);
                                        }

                                        reportDetail = new
                                        {
                                            ReportId = report.Id,
                                            ReporterId = report.ReporterId,
                                            ReporterName = reporterDisplayName,
                                            ReportedType = report.ReportedType.ToString(),
                                            Message = report.Message ?? "",
                                            CreatedAt = report.CreatedAt,
                                            Index = index + ((page - 1) * pageSize),
                                            ReportedName = $"{user.FirstName} {user.LastName}",
                                            ReportedId = user.Id,
                                            Images = images
                                        };
                                    }
                                }
                                break;
                        }

                        reportDetails.Add(reportDetail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing report {report.Id}");
                        // Continue to the next report rather than failing the entire request
                    }
                }

                // Return the results with pagination metadata
                return Ok(new
                {
                    Reports = reportDetails,
                    TotalReports = totalReports,
                    TotalPages = totalPages,
                    CurrentPage = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports");
                return StatusCode(500, new { Message = "An error occurred while retrieving reports", Error = ex.Message });
            }
        }

        [HttpGet("GetNumOfProducts")]
        public async Task<IActionResult> GetNumberOfProduct([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Authenticate and authorize the user as an admin
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can get the number of products." });
            }

            // Get the count of products
            var number = await _unitOfWork.Product.CountAsync();

            return Ok(new
            {
                ProductCount = number
            });
        }

        [HttpGet("GetNumOfUsers")]
        public async Task<IActionResult> GetNumberOfUsers([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Authenticate and authorize the user as an admin
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can get the number of users." });
            }

            // Get the count of users
            var number = await _unitOfWork.AppUser.CountAsync();

            return Ok(new
            {
                UserCount = number
            });
        }
        [HttpGet("GetNumOfReports")]
        public async Task<IActionResult> GetNumberOfReports([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Authenticate and authorize the user as an admin
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can get the number of users." });
            }

            // Get the count of users
            var number = await _unitOfWork.Report.CountAsync();

            return Ok(new
            {
                ReportCount = number
            });
        }

        [HttpGet("GetNumOfDeals")]
        public async Task<IActionResult> GetNumberOfDeals([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Authenticate and authorize the user as an admin
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can get the number of deals." });
            }

            // Get the count of deals
            var number = await _unitOfWork.Deal.CountAsync();

            return Ok(new
            {
                DealCount = number
            });
        }


        [HttpGet("getAllUsers")]
        public async Task<IActionResult> GetAllUsers(
     [FromHeader(Name = "Authorization")] string authorizationHeader,
     int page = 1,
     int pageSize = 10,
     string roleName = null,
     string roleId = null,
     string searchUsername = null,
     string searchEmail = null)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(new { Message = "Page number and page size must be greater than 0." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
                return Unauthorized(new { Message = "Only admin users can retrieve all users." });

            List<GetAllUsersVM> resultUserModels = new();
            int resultTotalUsers;

            List<AppUser> usersToFilter;

            // Get users by role if specified
            if (!string.IsNullOrEmpty(roleId) || !string.IsNullOrEmpty(roleName))
            {
                string targetRoleName = null;

                if (!string.IsNullOrEmpty(roleId))
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role == null)
                        return BadRequest(new { Message = $"Role with ID '{roleId}' not found." });

                    targetRoleName = role.Name;
                }
                else
                {
                    var roleExists = await _roleManager.RoleExistsAsync(roleName);
                    if (!roleExists)
                        return BadRequest(new { Message = $"Role with name '{roleName}' not found." });

                    targetRoleName = roleName;
                }

                usersToFilter = (await _userManager.GetUsersInRoleAsync(targetRoleName)).ToList();
            }
            else
            {
                usersToFilter = await _unitOfWork.AppUser.GetAllUsersAsync();
            }

            // Apply filters
            searchUsername = searchUsername?.Trim().ToLowerInvariant();
            searchEmail = searchEmail?.Trim().ToLowerInvariant();

            var filteredUsers = usersToFilter.Where(u =>
    (string.IsNullOrEmpty(searchUsername) ||
     string.Concat(u.FirstName, u.LastName).Replace(" ", "").ToLowerInvariant()
        .Contains(searchUsername.Replace(" ", ""))) &&
    (string.IsNullOrEmpty(searchEmail) ||
     u.Email.ToLowerInvariant().Contains(searchEmail))
).ToList();


            resultTotalUsers = filteredUsers.Count;

            var pagedUsers = filteredUsers
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            for (int i = 0; i < pagedUsers.Count; i++)
            {
                var user = pagedUsers[i];
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Unknown";

                resultUserModels.Add(new GetAllUsersVM
                {
                    Index = i,
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    City = user.City,
                    District = user.District,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = role,
                    IsBlocked = user.Isblock,
                    RegisteredAt = user.RegisteredAt.ToString("yyyy-MM-dd HH:mm")
                });
            }

            return Ok(new
            {
                TotalUsers = resultTotalUsers,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(resultTotalUsers / (double)pageSize),
                Users = resultUserModels
            });
        }

        [HttpPost("block/{userId}")]
        public async Task<IActionResult> BlockUser(
      [FromHeader(Name = "Authorization")] string authorizationHeader,
      [FromRoute] string userId)
        {
            // Ensure only an admin can perform this action
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can block users." });
            }

            // Find the user to be blocked
            var blockedUser = await _userManager.FindByIdAsync(userId);
            if (blockedUser == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            // Check if already blocked
            if (blockedUser.Isblock)
            {
                return BadRequest(new { Message = "User is already blocked." });
            }

            // Block the user
            blockedUser.Isblock = true;
            var result = await _userManager.UpdateAsync(blockedUser);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to block user.", Errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new
            {
                Message = "User has been successfully blocked.",
                UserId = blockedUser.Id,
                Email = blockedUser.Email,
                Isblock = blockedUser.Isblock
            });
        }

        [HttpPost("unblock/{userId}")]
        public async Task<IActionResult> UnBlockUser(
     [FromHeader(Name = "Authorization")] string authorizationHeader,
     [FromRoute] string userId)
        {
            // Ensure only an admin can perform this action
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can unblock users." });
            }

            // Find the user to be unblocked
            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            // Check if already unblocked
            if (!targetUser.Isblock)
            {
                return BadRequest(new { Message = "User is already unblocked." });
            }

            // Unblock the user
            targetUser.Isblock = false;
            var result = await _userManager.UpdateAsync(targetUser);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to unblock user.", Errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new
            {
                Message = "User has been successfully unblocked.",
                UserId = targetUser.Id,
                Email = targetUser.Email,
                Isblock = targetUser.Isblock
            });
        }

        [HttpDelete("useraccount/{userId}")]
        public async Task<IActionResult> DeleteUserAccount(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromRoute] string userId)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can delete users." });
            }

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            var result = await _userManager.DeleteAsync(targetUser);
            if (result.Succeeded)
            {
                return Ok(new { Message = "User account deleted successfully." });
            }

            return BadRequest(new { Message = "Failed to delete user.", Errors = result.Errors });
        }

        [HttpPatch("useraccount/{userId}")]
        public async Task<IActionResult> EditUserAccount(
     [FromHeader(Name = "Authorization")] string authorizationHeader,
     [FromRoute] string userId,
     [FromBody] EditUserAccountDTO userAccountDTO)
        {
            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can edit users." });
            }

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            // Apply updates if provided
            if (!string.IsNullOrWhiteSpace(userAccountDTO.FirstName))
                targetUser.FirstName = userAccountDTO.FirstName;

            if (!string.IsNullOrWhiteSpace(userAccountDTO.LastName))
                targetUser.LastName = userAccountDTO.LastName;

            if (!string.IsNullOrWhiteSpace(userAccountDTO.City))
                targetUser.City = userAccountDTO.City;

            if (!string.IsNullOrWhiteSpace(userAccountDTO.District))
                targetUser.District = userAccountDTO.District;

            if (!string.IsNullOrWhiteSpace(userAccountDTO.Country))
                targetUser.Country = userAccountDTO.Country;

            if (!string.IsNullOrWhiteSpace(userAccountDTO.PhoneNumber))
                targetUser.PhoneNumber = userAccountDTO.PhoneNumber;

            var result = await _userManager.UpdateAsync(targetUser);
            if (result.Succeeded)
            {
                return Ok(new { Message = "User account updated successfully." });
            }

            return BadRequest(new { Message = "Failed to update user.", Errors = result.Errors });
        }

        [HttpGet("deals-need-admin-approval")]
        public async Task<IActionResult> GetDealsNeedAdminApproval(
            [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can access this information." });
            }

            var dealsNeedAdminApproval = await _unitOfWork.Deal.GetDealsNeedAdminApproval();

            var response = dealsNeedAdminApproval.Select(deal => new GetDealsNeedAdminApprovalDTO
            {
                DealId = deal.Id,
                BusinessName = deal.BusinessName,
                AuthorId = deal.AuthorId!,
                AuthorName = $"{deal.Author?.FirstName ?? "Unknown"} {deal.Author?.LastName ?? "Unknown"}".Trim(),
                InvestorId = deal.InvestorId!,
                InvestorName = $"{deal.Investor?.FirstName ?? "Unknown"} {deal.Investor?.LastName ?? "Unknown"}".Trim(),
                CategoryId = deal.CategoryId,
                CategoryName = deal.Category?.Name ?? "Uncategorized",
                OfferMoney = deal.OfferMoney,
                OfferDeal = deal.OfferDeal,
                ManufacturingCost = deal.ManufacturingCost,
                EstimatedPrice = deal.EstimatedPrice,
                AdminApproved = false
            }).ToList();

            return Ok(response);
        }

        [HttpGet("product-linked-deals/{userId}")]
        public async Task<IActionResult> GetProductLinkedDeals(
    [FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            var currentUser = await _unitOfWork.Auth.AuthenticateAndAuthorizeUser(authorizationHeader, "Admin");
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "Only admins can access this information." });
            }

            var dealLinkedProducts = await _unitOfWork.Product.GetProductLinkedDeals(userId);

            var response = dealLinkedProducts.Select(p => new
            {
                ProductId = p.Id,
                ProductName = p.Name,
                AuthorName = $"{p.Author.FirstName} {p.Author.LastName}",
                Price = p.Price,
                Stock = p.Stock,
                Deals = p.Deals.Select(d => new
                {
                    DealId = d.Id,
                    InvestorName = $"{d.Investor!.FirstName} {d.Investor.LastName}",
                    EquityPercentage = d.OfferDeal,
                    DealAmount = d.OfferMoney
                })
            }).ToList();

            return Ok(response);
        }

        [HttpPost("analyze-business-owner/{userId}")]
        public async Task<IActionResult> AnalyzeUserForSpam(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            string userId)
        {
            try
            {
                var currentUserId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized(new { Message = "Invalid token or user not found." });

                var isAdmin = await _unitOfWork.Auth.IsAdmin(currentUserId);
                if (!isAdmin)
                {
                    return Forbid("Only admins can see this information.");
                }

                // ✅ FLASK ONLY - Will throw exception if Flask fails
                var analysis = await _mlSpamDetectionService.AnalyzeUserAsync(userId);

                return Ok(new
                {
                    UserId = userId,
                    Analysis = new
                    {
                        IsSpam = analysis.IsSpam,
                        Prediction = analysis.Prediction,
                        ConfidenceScore = analysis.ConfidenceScore,
                        RecommendedAction = analysis.RecommendedAction,
                        ProfileMetrics = analysis.InputFeatures,
                        Timestamp = analysis.Timestamp
                    }
                });
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Flask ML API failed for spam detection");
                return ServiceUnavailable(new
                {
                    Message = "ML Spam Detection service is currently unavailable",
                    Error = ex.Message,
                    Source = "Flask ML API Error"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing user for spam via Flask");
                return StatusCode(500, new { Message = "Error analyzing user", Error = ex.Message });
            }
        }

        private ObjectResult ServiceUnavailable(object value)
        {
            return StatusCode(503, value); // 503 Service Unavailable
        }
    }
}