using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public ReportController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }
        [HttpPost]
        public async Task<IActionResult> CreateReport(
         [FromHeader(Name = "Authorization")] string authorizationHeader,
         [FromBody] CreateReportDto createReport)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized(new { Message = "User not found." });

            if (string.IsNullOrWhiteSpace(createReport.Type))
                return BadRequest(new { Message = "Report type is required." });

            if (!Enum.TryParse(createReport.Type, true, out ReportedEntityType reportedType))
                return BadRequest(new { Message = "Invalid report type." });

            object target = null;
            string reportedId = null;

            switch (reportedType)
            {
                case ReportedEntityType.User:
                    if (!Guid.TryParse(createReport.TargetId, out Guid userGuid))
                        return BadRequest(new { Message = "Invalid GUID for user ID." });

                    target = await _unitOfWork.AppUser.GetUSerByIdAsync(userGuid.ToString());
                    reportedId = userGuid.ToString();
                    break;

                case ReportedEntityType.Deal:
                    if (!int.TryParse(createReport.TargetId, out int dealId))
                        return BadRequest(new { Message = "TargetId should be an integer for Deal." });

                    target = await _unitOfWork.Deal.GetByIdAsync(dealId);
                    reportedId = dealId.ToString();
                    break;

                case ReportedEntityType.Product:
                    if (!int.TryParse(createReport.TargetId, out int productId))
                        return BadRequest(new { Message = "TargetId should be an integer for Product." });

                    target = await _unitOfWork.Product.GetByIdAsync(productId);
                    reportedId = productId.ToString();
                    break;
            }

            if (target == null)
                return NotFound(new { Message = $"{createReport.Type} with ID {createReport.TargetId} not found." });

            var report = new Report
            {
                ReporterId = user.Id,
                ReportedType = reportedType,
                ReportedId = reportedId,
                Message = createReport.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Report.AddAsync(report);
            await _unitOfWork.Complete();

            return Ok(new { Message = "Report created successfully." });
        }

      
    }
}
