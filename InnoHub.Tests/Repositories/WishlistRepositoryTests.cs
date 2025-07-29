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
    public class WishlistRepositoryTests : BaseRepositoryTest
    {
        private readonly WishlistRepository _wishlistRepository;

        public WishlistRepositoryTests()
        {
            _wishlistRepository = new WishlistRepository(Context);
        }

        [Fact]
        public async Task GetWishlistByUserID_ShouldReturnUserWishlist()
        {
            // Arrange
            await SeedTestDataAsync();
            var wishlist = TestDataHelper.CreateTestWishlist("test-user-id");
            Context.Wishlists.Add(wishlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _wishlistRepository.GetWishlistByUserID("test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("test-user-id");
            result.WishlistItems.Should().NotBeEmpty();
        }

        [Fact]
        public async Task RemoveProductFromWishlist_ShouldRemoveProductSuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);

            var wishlist = TestDataHelper.CreateTestWishlist("test-user-id");
            Context.Wishlists.Add(wishlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _wishlistRepository.RemoveProductFromWishlist(1, "test-user-id");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveProductFromWishlist_WithNonExistingProduct_ShouldReturnFalse()
        {
            // Arrange
            await SeedTestDataAsync();
            var wishlist = TestDataHelper.CreateTestWishlist("test-user-id");
            Context.Wishlists.Add(wishlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _wishlistRepository.RemoveProductFromWishlist(999, "test-user-id");

            // Assert
            result.Should().BeFalse();
        }
    }
}