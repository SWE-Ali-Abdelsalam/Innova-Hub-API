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
    public class ProductCommentRepositoryTests : BaseRepositoryTest
    {
        private readonly ProductCommentRepository _productCommentRepository;

        public ProductCommentRepositoryTests()
        {
            _productCommentRepository = new ProductCommentRepository(Context);
        }

        [Fact]
        public async Task GetCommentsByProductIdAsync_ShouldReturnProductComments()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);

            var comment = new ProductComment
            {
                Id = 1,
                ProductId = 1,
                UserId = "test-user-id",
                CommentText = "Great product!",
                CreatedAt = DateTime.UtcNow
            };
            Context.ProductComments.Add(comment);
            await Context.SaveChangesAsync();

            // Act
            var result = await _productCommentRepository.GetCommentsByProductIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().CommentText.Should().Be("Great product!");
        }

        [Fact]
        public async Task GetCommentsByProductIdAsync_WithNoComments_ShouldReturnEmpty()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _productCommentRepository.GetCommentsByProductIdAsync(999);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}