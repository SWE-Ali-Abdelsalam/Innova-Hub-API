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
    public class DealProfitRepositoryTests : BaseRepositoryTest
    {
        private readonly DealProfitRepository _dealProfitRepository;

        public DealProfitRepositoryTests()
        {
            _dealProfitRepository = new DealProfitRepository(Context);
        }

        [Fact]
        public async Task GetProfitsByDealId_ShouldReturnDealProfits()
        {
            // Arrange
            await SeedTestDataAsync();
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            Context.Deals.Add(deal);

            var profit = new DealProfit
            {
                Id = 1,
                DealId = 1,
                TotalRevenue = 1000m,
                ManufacturingCost = 500m,
                OtherCosts = 100m,
                NetProfit = 400m,
                InvestorShare = 80m,
                OwnerShare = 320m,
                PlatformFee = 8m,
                DistributionDate = DateTime.UtcNow,
                IsPaid = false
            };
            Context.DealProfits.Add(profit);
            await Context.SaveChangesAsync();

            // Act
            var result = await _dealProfitRepository.GetProfitsByDealId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().NetProfit.Should().Be(400m);
        }
    }
}