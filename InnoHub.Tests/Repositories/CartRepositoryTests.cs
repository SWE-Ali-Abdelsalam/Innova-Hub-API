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
    public class CartRepositoryTests : BaseRepositoryTest
    {
        private readonly CartRepository _cartRepository;

        public CartRepositoryTests()
        {
            _cartRepository = new CartRepository(Context);
        }

        [Fact]
        public async Task GetCartBYUserId_ShouldReturnUserCart()
        {
            // Arrange
            await SeedTestDataAsync();
            var cart = TestDataHelper.CreateTestCart("test-user-id");
            Context.Carts.Add(cart);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.GetCartBYUserId("test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("test-user-id");
            result.CartItems.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateCart_WithValidProduct_ShouldCreateCartSuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.CreateCart("test-user-id", 1, 2);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("test-user-id");
            result.CartItems.Should().HaveCount(1);
            result.CartItems.First().Quantity.Should().Be(2);
            result.CartItems.First().ProductId.Should().Be(1);
        }

        [Fact]
        public async Task CreateCart_WithInsufficientStock_ShouldReturnNull()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            product.Stock = 1; // Set stock to 1
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.CreateCart("test-user-id", 1, 5); // Try to add 5 items

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CheckIfProductExistsInCart_WithExistingProduct_ShouldReturnTrue()
        {
            // Arrange
            await SeedTestDataAsync();
            var cart = TestDataHelper.CreateTestCart("test-user-id");
            Context.Carts.Add(cart);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.CheckIfProductExistsInCart("test-user-id", 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckIfProductExistsInCart_WithNonExistingProduct_ShouldReturnFalse()
        {
            // Arrange
            await SeedTestDataAsync();
            var cart = TestDataHelper.CreateTestCart("test-user-id");
            Context.Carts.Add(cart);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.CheckIfProductExistsInCart("test-user-id", 999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteProductFromCart_ShouldRemoveProductSuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var cart = TestDataHelper.CreateTestCart("test-user-id");
            Context.Carts.Add(cart);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.DeleteProductFromCart("test-user-id", 1);

            // Assert
            result.Should().NotBeNull();
            result.CartItems.Should().BeEmpty();
            result.TotalPrice.Should().Be(0);
        }

        [Fact]
        public async Task ClearCart_ShouldRemoveAllItems()
        {
            // Arrange
            await SeedTestDataAsync();
            var cart = TestDataHelper.CreateTestCart("test-user-id");
            Context.Carts.Add(cart);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.ClearCart("test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.CartItems.Should().BeEmpty();
            result.TotalPrice.Should().Be(0);
        }

        [Fact]
        public async Task UpdateProductQuantity_ShouldUpdateQuantityAndTotalPrice()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            product.Stock = 20; // Ensure sufficient stock
            Context.Products.Add(product);

            var cart = TestDataHelper.CreateTestCart("test-user-id");
            Context.Carts.Add(cart);
            await Context.SaveChangesAsync();

            // Act
            var result = await _cartRepository.UpdateProductQuantity("test-user-id", 1, 1); // Add 1 more

            // Assert
            result.Should().NotBeNull();
            result.CartItems.First().Quantity.Should().Be(3); // Was 2, now 3
            result.TotalPrice.Should().Be(300m); // 3 * 100
        }
    }
}