using FluentAssertions;
using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Repositories
{
    public class NotificationRepositoryTests : BaseRepositoryTest
    {
        private readonly NotificationRepository _notificationRepository;

        public NotificationRepositoryTests()
        {
            _notificationRepository = new NotificationRepository(Context);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnUserNotifications()
        {
            // Arrange
            await SeedTestDataAsync();
            var notification = new NotificationMessage
            {
                Id = 1,
                UserId = "test-user-id",
                Title = "Test Notification",
                Message = "This is a test notification",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Type = NotificationType.General
            };
            Context.Notifications.Add(notification);
            await Context.SaveChangesAsync();

            // Act
            var result = await _notificationRepository.GetByUserIdAsync("test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Test Notification");
        }

        [Fact]
        public async Task GetUnreadCountByUserIdAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            await SeedTestDataAsync();
            var notification1 = new NotificationMessage
            {
                Id = 1,
                UserId = "test-user-id",
                Title = "Unread Notification",
                Message = "This is unread",
                IsRead = false,
                Type = NotificationType.General
            };

            var notification2 = new NotificationMessage
            {
                Id = 2,
                UserId = "test-user-id",
                Title = "Read Notification",
                Message = "This is read",
                IsRead = true,
                Type = NotificationType.General
            };

            Context.Notifications.AddRange(notification1, notification2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _notificationRepository.GetUnreadCountByUserIdAsync("test-user-id");

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead()
        {
            // Arrange
            await SeedTestDataAsync();
            var notification = new NotificationMessage
            {
                Id = 1,
                UserId = "test-user-id",
                Title = "Test Notification",
                Message = "This is a test",
                IsRead = false,
                Type = NotificationType.General
            };
            Context.Notifications.Add(notification);
            await Context.SaveChangesAsync();

            // Act
            await _notificationRepository.MarkAsReadAsync(1);

            // Assert
            var updatedNotification = await Context.Notifications.FindAsync(1);
            updatedNotification.IsRead.Should().BeTrue();
        }
    }
}