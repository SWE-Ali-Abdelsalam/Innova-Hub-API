using InnoHub.Core.Models;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Tests.Repositories
{
    public class GenericRepositoryTests : BaseRepositoryTest
    {
        private readonly GenericRepository<Category> _repository;

        public GenericRepositoryTests()
        {
            _repository = new GenericRepository<Category>(Context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddEntitySuccessfully()
        {
            // Arrange
            var category = TestDataHelper.CreateTestCategory(10);

            // Act
            var result = await _repository.AddAsync(category);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectEntity()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntitySuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();
            var category = await _repository.GetByIdAsync(1);
            category.Name = "Updated Category";

            // Act
            var result = await _repository.UpdateAsync(category);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Category");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteEntitySuccessfully()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            var deletedEntity = await _repository.GetByIdAsync(1);
            deletedEntity.Should().BeNull();
        }

        [Fact]
        public async Task CountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _repository.CountAsync();

            // Assert
            result.Should().BeGreaterThan(0);
        }
    }
}
