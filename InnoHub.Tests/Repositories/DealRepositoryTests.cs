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
    public class DealRepositoryTests : BaseRepositoryTest
    {
        private readonly DealRepository _dealRepository;

        public DealRepositoryTests()
        {
            _dealRepository = new DealRepository(Context);
        }

        [Fact]
        public async Task GetDealsByApprovalAsync_ShouldReturnApprovedDeals()
        {
            // Arrange
            await SeedTestDataAsync();
            var approvedDeal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            approvedDeal.IsApproved = true;

            var unapprovedDeal = TestDataHelper.CreateTestDeal(2, "test-user-id", "investor-id");
            unapprovedDeal.IsApproved = false;

            Context.Deals.AddRange(approvedDeal, unapprovedDeal);
            await Context.SaveChangesAsync();

            // Act
            var result = await _dealRepository.GetDealsByApprovalAsync(1, 10, true);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().IsApproved.Should().BeTrue();
        }

        [Fact]
        public async Task GetDealWithDetails_ShouldReturnDealWithRelatedData()
        {
            // Arrange
            await SeedTestDataAsync();
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            Context.Deals.Add(deal);
            await Context.SaveChangesAsync();

            // Act
            var result = await _dealRepository.GetDealWithDetails(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.AuthorId.Should().Be("test-user-id");
            result.InvestorId.Should().Be("investor-id");
        }

        [Fact]
        public async Task GetDealsByOwnerId_ShouldReturnOwnerDeals()
        {
            // Arrange
            await SeedTestDataAsync();
            var deal1 = TestDataHelper.CreateTestDeal(1, "owner1", "investor-id");
            var deal2 = TestDataHelper.CreateTestDeal(2, "owner1", "investor-id");
            var deal3 = TestDataHelper.CreateTestDeal(3, "owner2", "investor-id");

            Context.Deals.AddRange(deal1, deal2, deal3);
            await Context.SaveChangesAsync();

            // Act
            var result = await _dealRepository.GetDealsByOwnerId("owner1");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(d => d.AuthorId == "owner1").Should().BeTrue();
        }

        [Fact]
        public async Task HasActiveDealsAsync_WithActiveDeal_ShouldReturnTrue()
        {
            // Arrange
            await SeedTestDataAsync();
            var activeDeal = TestDataHelper.CreateTestDeal(1, "author-id", "investor-id");
            activeDeal.Status = DealStatus.Active;
            Context.Deals.Add(activeDeal);
            await Context.SaveChangesAsync();

            // Act
            var result = await _dealRepository.HasActiveDealsAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasActiveDealsAsync_WithInactiveDeal_ShouldReturnFalse()
        {
            // Arrange
            await SeedTestDataAsync();
            var inactiveDeal = TestDataHelper.CreateTestDeal(1, "author-id", "investor-id");
            inactiveDeal.Status = DealStatus.Completed;
            Context.Deals.Add(inactiveDeal);
            await Context.SaveChangesAsync();

            // Act
            var result = await _dealRepository.HasActiveDealsAsync(1);

            // Assert
            result.Should().BeFalse();
        }
    }
}