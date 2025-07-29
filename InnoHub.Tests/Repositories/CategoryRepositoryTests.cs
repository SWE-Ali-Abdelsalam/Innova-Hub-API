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
    public class CategoryRepositoryTests : BaseRepositoryTest
    {
        private readonly CategoryRepository _categoryRepository;

        public CategoryRepositoryTests()
        {
            _categoryRepository = new CategoryRepository(Context);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCategories()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _categoryRepository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetAuthorNamesByIdsAsync_ShouldReturnCorrectNames()
        {
            // Arrange
            await SeedTestDataAsync();
            var authorIds = new[] { "test-user-id" };

            // Act
            var result = await _categoryRepository.GetAuthorNamesByIdsAsync(authorIds);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("test-user-id");
            result["test-user-id"].Should().Be("Test User");
        }
    }
}