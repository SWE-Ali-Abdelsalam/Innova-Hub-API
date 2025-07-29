using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Repositories
{
    public class DealChangeRequestRepositoryTests : BaseRepositoryTest
    {
        private readonly DealChangeRequestRepository _repository;
        public DealChangeRequestRepositoryTests()
        {
            _repository = new DealChangeRequestRepository(Context);
        }

        [Fact]
        public async Task GetWithDetailsAsync_ShouldReturnRequestWithDeal()
        {
            // Arrange
            await SeedTestDataAsync();
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            Context.Deals.Add(deal);

            var changeRequest = new DealChangeRequest
            {
                Id = 1,
                DealId = 1,
                RequestedById = "test-user-id",
                OriginalValues = "{}",
                RequestedValues = "{}",
                Status = ChangeRequestStatus.Pending,
                //CreatedAt = DateTime.UtcNow
            };
            Context.DealChangeRequests.Add(changeRequest);
            await Context.SaveChangesAsync();

            // Act
            var result = await _repository.GetWithDetailsAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Deal.Should().NotBeNull();
            result.Status.Should().Be(ChangeRequestStatus.Pending);
        }
    }
}