using FluentAssertions;
using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Repositories
{
    public class ProductRatingRepositoryTests : BaseRepositoryTest
    {
        private readonly ProductRatingRepository _productRatingRepository;

        public ProductRatingRepositoryTests()
        {
            _productRatingRepository = new ProductRatingRepository(Context);
        }

        [Fact]
        public async Task GetRatingByProductIdAndUserIdAsync_ShouldReturnRating()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);

            var rating = new ProductRating
            {
                ProductId = 1,
                UserId = "test-user-id",
                RatingValue = 5,
                CreatedAt = DateTime.UtcNow
            };
            Context.ProductRatings.Add(rating);
            await Context.SaveChangesAsync();

            // Act
            var result = await _productRatingRepository.GetRatingByProductIdAndUserIdAsync(1, "test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.RatingValue.Should().Be(5);
            result.ProductId.Should().Be(1);
            result.UserId.Should().Be("test-user-id");
        }

        [Fact]
        public async Task GetRatingByProductIdAndUserIdAsync_WithNoRating_ShouldReturnNull()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _productRatingRepository.GetRatingByProductIdAndUserIdAsync(999, "test-user-id");

            // Assert
            result.Should().BeNull();
        }
    }
}