using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InnoHub.ModelDTO;
using InnoHub.Core.Models;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(IUnitOfWork unitOfWork, ILogger<NotificationController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// جلب تاريخ الإشعارات مع فلترة وبحث
        /// (للصفحات اللي بتعرض تاريخ الإشعارات)
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetNotificationHistory(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? messageType = null,
            [FromQuery] bool? isRead = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            try
            {
                var messages = await _unitOfWork.InvestmentMessage.GetMessagesByRecipientId(userId, false);//
                var messagesList = messages.ToList();

                // تطبيق الفلاتر
                if (fromDate.HasValue)
                    messagesList = messagesList.Where(m => m.CreatedAt >= fromDate.Value).ToList();

                if (toDate.HasValue)
                    messagesList = messagesList.Where(m => m.CreatedAt <= toDate.Value).ToList();

                if (!string.IsNullOrEmpty(messageType))
                    messagesList = messagesList.Where(m => m.MessageType.ToString() == messageType).ToList();

                if (isRead.HasValue)
                    messagesList = messagesList.Where(m => m.IsRead == isRead.Value).ToList();

                // الترقيم
                var totalCount = messagesList.Count;
                var paginatedMessages = messagesList
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        Id = m.Id,
                        DealId = m.DealId,
                        SenderId = m.SenderId,
                        SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                        MessageText = m.MessageText,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "Not Determined",
                        MessageType = m.MessageType.ToString()
                    })
                    .ToList();

                return Ok(new
                {
                    Message = "Notification history retrieved successfully.",
                    Data = paginatedMessages,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    },
                    Filters = new
                    {
                        FromDate = fromDate,
                        ToDate = toDate,
                        MessageType = messageType,
                        IsRead = isRead
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notification history for user {userId}");
                return StatusCode(500, new { Message = "Error retrieving notification history." });
            }
        }

        /// <summary>
        /// تحديد إشعارات متعددة كمقروءة
        /// </summary>
        [HttpPut("mark-multiple-read")]
        public async Task<IActionResult> MarkMultipleAsRead(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] MarkNotificationsReadDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            if (!request.NotificationIds.Any())
                return BadRequest(new { Message = "No notification IDs provided." });

            try
            {
                var updatedCount = 0;
                foreach (var messageId in request.NotificationIds)
                {
                    var message = await _unitOfWork.InvestmentMessage.GetByIdAsync(messageId);
                    if (message != null && message.RecipientId == userId && !message.IsRead)
                    {
                        message.IsRead = true;
                        await _unitOfWork.InvestmentMessage.UpdateAsync(message);
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    await _unitOfWork.Complete();
                }

                return Ok(new
                {
                    Message = "Notifications marked as read successfully.",
                    UpdatedCount = updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking multiple notifications as read for user {userId}");
                return StatusCode(500, new { Message = "Error marking notifications as read." });
            }
        }

        /// <summary>
        /// حذف إشعارات متعددة
        /// </summary>
        [HttpDelete("delete-multiple")]
        public async Task<IActionResult> DeleteMultipleNotifications(
            [FromHeader(Name = "Authorization")] string authorizationHeader,
            [FromBody] MarkNotificationsReadDTO request)
        {
            var userId = _unitOfWork.Auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token or user not found." });

            if (!request.NotificationIds.Any())
                return BadRequest(new { Message = "No notification IDs provided." });

            try
            {
                var deletedCount = 0;
                foreach (var messageId in request.NotificationIds)
                {
                    var message = await _unitOfWork.InvestmentMessage.GetByIdAsync(messageId);
                    if (message != null && message.RecipientId == userId)
                    {
                        await _unitOfWork.InvestmentMessage.DeleteAsync(messageId);
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    await _unitOfWork.Complete();
                }

                return Ok(new
                {
                    Message = "Notifications deleted successfully.",
                    DeletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting multiple notifications for user {userId}");
                return StatusCode(500, new { Message = "Error deleting notifications." });
            }
        }
    }
}