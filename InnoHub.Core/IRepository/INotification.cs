using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface INotification : IGenericRepository<NotificationMessage>
    {
        Task<IEnumerable<NotificationMessage>> GetByUserIdAsync(string userId, bool unreadOnly = false);
        Task<int> GetUnreadCountByUserIdAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}
