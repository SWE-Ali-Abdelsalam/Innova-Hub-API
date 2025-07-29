using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using FluentAssertions;

namespace InnoHub.Tests.Repositories
{
    public class PaymentRefundLogRepositoryTests : BaseRepositoryTest
    {
        private readonly PaymentRefundLogRepository _repository;

        public PaymentRefundLogRepositoryTests()
        {
            _repository = new PaymentRefundLogRepository(Context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddRefundLogSuccessfully()
        {
            // Arrange
            var refundLog = new PaymentRefundLog
            {
                OrderId = 1,
                RefundAmount = 100m,
                RefundId = "re_test123",
                RefundStatus = "succeeded",
                RefundCreated = DateTime.UtcNow
            };

            // Act
            var result = await _repository.AddAsync(refundLog);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.RefundAmount.Should().Be(100m);
        }
    }
}
