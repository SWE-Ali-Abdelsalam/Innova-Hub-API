using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Tests.Repositories
{
    public class DealTransactionRepositoryTests : BaseRepositoryTest
    {
        private readonly DealTransactionRepository _repository;

        public DealTransactionRepositoryTests()
        {
            _repository = new DealTransactionRepository(Context);
        }

        [Fact]
        public async Task GetTransactionsByDealId_ShouldReturnDealTransactions()
        {
            // Arrange
            await SeedTestDataAsync();
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            Context.Deals.Add(deal);

            var transaction = new DealTransaction
            {
                Id = 1,
                DealId = 1,
                Amount = 1000m,
                Type = TransactionType.InitialInvestment,
                TransactionId = "txn_123",
                Description = "Investment payment"
            };
            Context.DealTransactions.Add(transaction);
            await Context.SaveChangesAsync();

            // Act
            var result = await _repository.GetTransactionsByDealId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Amount.Should().Be(1000m);
        }
    }
}
