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
    public class ProductRepositoryTests : BaseRepositoryTest
    {
        private readonly ProductRepository _productRepository;

        public ProductRepositoryTests()
        {
            _productRepository = new ProductRepository(Context);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllProducts()
        {
            // Arrange
            await SeedTestDataAsync();
            var product1 = TestDataHelper.CreateTestProduct(1, "test-user-id");
            var product2 = TestDataHelper.CreateTestProduct(2, "test-user-id");

            Context.Products.AddRange(product1, product2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _productRepository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Id == 1);
            result.Should().Contain(p => p.Id == 2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectProduct()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Act
            var result = await _productRepository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test Product");
            result.Price.Should().Be(100m);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _productRepository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldAddProductSuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");

            // Act
            var result = await _productRepository.AddAsync(product);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);

            var savedProduct = await Context.Products.FindAsync(result.Id);
            savedProduct.Should().NotBeNull();
            savedProduct.Name.Should().Be("Test Product");
        }

        [Fact]
        public async Task GetAllProductsByCategoryId_ShouldReturnProductsForSpecificCategory()
        {
            // Arrange
            await SeedTestDataAsync();
            var product1 = TestDataHelper.CreateTestProduct(1, "test-user-id");
            product1.CategoryId = 1;
            var product2 = TestDataHelper.CreateTestProduct(2, "test-user-id");
            product2.CategoryId = 2;

            Context.Products.AddRange(product1, product2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _productRepository.GetAllProductsByCategoryId(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().CategoryId.Should().Be(1);
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldUpdateProductSuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Modify product
            product.Name = "Updated Product Name";
            product.Price = 150m;

            // Act
            var result = await _productRepository.UpdateProductAsync(product);

            // Assert
            result.Should().BeTrue();

            var updatedProduct = await Context.Products.FindAsync(1);
            updatedProduct.Name.Should().Be("Updated Product Name");
            updatedProduct.Price.Should().Be(150m);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveProductSuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var product = TestDataHelper.CreateTestProduct(1, "test-user-id");
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Act
            var result = await _productRepository.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            var deletedProduct = await Context.Products.FindAsync(1);
            deletedProduct.Should().BeNull();
        }
    }
}