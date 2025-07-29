using FluentAssertions;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Repositories
{
    public class DeliveryMethodRepositoryTests : BaseRepositoryTest
    {
        private readonly DeliveryMethodRepository _deliveryMethodRepository;

        public DeliveryMethodRepositoryTests()
        {
            _deliveryMethodRepository = new DeliveryMethodRepository(Context);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllDeliveryMethods()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _deliveryMethodRepository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectDeliveryMethod()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _deliveryMethodRepository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.ShortName.Should().Be("Standard");
        }
    }
}