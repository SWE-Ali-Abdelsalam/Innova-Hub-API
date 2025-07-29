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
    public class DealMessageRepositoryTests : BaseRepositoryTest
    {
        private readonly DealMessageRepository _dealMessageRepository;

        public DealMessageRepositoryTests()
        {
            _dealMessageRepository = new DealMessageRepository(Context);
        }

        [Fact]
        public async Task GetMessagesByRecipientId_ShouldReturnRecipientMessages()
        {
            // Arrange
            await SeedTestDataAsync();
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            Context.Deals.Add(deal);

            var message1 = new DealMessage
            {
                Id = 1,
                DealId = 1,
                SenderId = "test-user-id",
                RecipientId = "investor-id",
                MessageText = "Message 1",
                IsRead = false,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            var message2 = new DealMessage
            {
                Id = 2,
                DealId = 1,
                SenderId = "test-user-id",
                RecipientId = "investor-id",
                MessageText = "Message 2",
                IsRead = true,
                MessageType = MessageType.General,
                CreatedAt = DateTime.UtcNow
            };

            Context.DealMessages.AddRange(message1, message2);
            await Context.SaveChangesAsync();

            // Act
            var allMessages = await _dealMessageRepository.GetMessagesByRecipientId("investor-id", false);
            var unreadMessages = await _dealMessageRepository.GetMessagesByRecipientId("investor-id", true);

            // Assert
            allMessages.Should().HaveCount(2);
            unreadMessages.Should().HaveCount(1);
            unreadMessages.First().IsRead.Should().BeFalse();
        }
    }
}