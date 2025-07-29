using FluentAssertions;
using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Repositories
{
    public class ShippingAddressRepositoryTests : BaseRepositoryTest
    {
        private readonly ShippingAddressRepository _shippingAddressRepository;

        public ShippingAddressRepositoryTests()
        {
            _shippingAddressRepository = new ShippingAddressRepository(Context);
        }

        [Fact]
        public async Task UserHasShippingAddress_WithExistingAddress_ShouldReturnTrue()
        {
            // Arrange
            await SeedTestDataAsync();
            var shippingAddress = TestDataHelper.CreateTestShippingAddress("test-user-id");
            Context.ShippingAddresses.Add(shippingAddress);
            await Context.SaveChangesAsync();

            // Act
            var result = await _shippingAddressRepository.UserHasShippingAddress("test-user-id");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UserHasShippingAddress_WithoutAddress_ShouldReturnFalse()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _shippingAddressRepository.UserHasShippingAddress("test-user-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetShippingAddressByUserId_ShouldReturnCorrectAddress()
        {
            // Arrange
            await SeedTestDataAsync();
            var shippingAddress = TestDataHelper.CreateTestShippingAddress("test-user-id");
            Context.ShippingAddresses.Add(shippingAddress);
            await Context.SaveChangesAsync();

            // Act
            var result = await _shippingAddressRepository.GetShippingAddressByUserId("test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("test-user-id");
            result.FirstName.Should().Be("Test");
            result.LastName.Should().Be("User");
        }
    }
}