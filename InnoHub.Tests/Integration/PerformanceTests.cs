using FluentAssertions;
using InnoHub.Core.Models;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Performance
{
    public class PerformanceTests : BaseRepositoryTest
    {
        [Fact]
        public async Task GetAllProducts_WithLargeDataset_ShouldCompleteInReasonableTime()
        {
            // Arrange
            await SeedTestDataAsync();
            var productRepository = new ProductRepository(Context);

            // Seed large dataset
            var products = new List<Product>();
            for (int i = 1; i <= 1000; i++)
            {
                products.Add(TestDataHelper.CreateTestProduct(i, "test-user-id"));
            }
            Context.Products.AddRange(products);
            await Context.SaveChangesAsync();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await productRepository.GetAllAsync();
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(1000);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task GetPaginatedProducts_ShouldReturnCorrectPage()
        {
            // Arrange
            await SeedTestDataAsync();
            var productRepository = new ProductRepository(Context);

            // Seed products
            var products = new List<Product>();
            for (int i = 1; i <= 50; i++)
            {
                products.Add(TestDataHelper.CreateTestProduct(i, "test-user-id"));
            }
            Context.Products.AddRange(products);
            await Context.SaveChangesAsync();

            // Act
            var result = await productRepository.GetPaginatedAsync(2, 10);

            // Assert
            result.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetProductsByCategory_WithLargeDataset_ShouldCompleteQuickly()
        {
            // Arrange
            await SeedTestDataAsync();
            var productRepository = new ProductRepository(Context);

            // Seed products across multiple categories
            var products = new List<Product>();
            for (int i = 1; i <= 500; i++)
            {
                var product = TestDataHelper.CreateTestProduct(i, "test-user-id");
                product.CategoryId = (i % 2) + 1; // Distribute between category 1 and 2
                products.Add(product);
            }
            Context.Products.AddRange(products);
            await Context.SaveChangesAsync();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await productRepository.GetAllProductsByCategoryId(1);
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(250); // Half of the products
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
        }
    }
}