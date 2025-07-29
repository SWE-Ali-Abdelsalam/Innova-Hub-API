using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using FluentAssertions;

namespace InnoHub.Tests.Repositories
{
    public class WishlistItemRepositoryTests : BaseRepositoryTest
    {
        private readonly WishlistItemRepository _repository;

        public WishlistItemRepositoryTests()
        {
            _repository = new WishlistItemRepository(Context);
        }

        [Fact]
        public async Task GetWishlistItemsByWishlistId_ShouldReturnWishlistItems()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);

            var wishlist = TestDataHelper.CreateTestWishlist("test-user-id");
            Context.Wishlists.Add(wishlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _repository.GetWishlistItemsByWishlistId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().ProductId.Should().Be(1);
        }
    }
}
