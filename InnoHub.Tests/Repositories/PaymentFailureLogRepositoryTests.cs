using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using FluentAssertions;

namespace InnoHub.Tests.Repositories
{
    public class PaymentFailureLogRepositoryTests : BaseRepositoryTest
    {
        private readonly PaymentFailureLogRepository _repository;

        public PaymentFailureLogRepositoryTests()
        {
            _repository = new PaymentFailureLogRepository(Context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddFailureLogSuccessfully()
        {
            // Arrange
            var failureLog = new PaymentFailureLog
            {
                UserId = "test-user-id",
                UserEmail = "test@example.com",
                PaymentIntentId = "pi_test123",
                FailureReason = "Insufficient funds",
                FailedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.AddAsync(failureLog);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.FailureReason.Should().Be("Insufficient funds");
        }
    }
}
