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
    public class OrderRepositoryTests : BaseRepositoryTest
    {
        private readonly OrderRepository _orderRepository;

        public OrderRepositoryTests()
        {
            _orderRepository = new OrderRepository(Context);
        }

        [Fact]
        public async Task GetAllOrdersForSpecificUser_WithInvalidUserId_ShouldReturnNull()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _orderRepository.GetAllOrdersForSpecificUser("");

            // Assert
            result.Should().BeNull();
        }
    }
}