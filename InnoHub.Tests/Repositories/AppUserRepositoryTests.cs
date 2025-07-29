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
    public class AppUserRepositoryTests : BaseRepositoryTest
    {
        private readonly AppUserRepository _appUserRepository;

        public AppUserRepositoryTests()
        {
            _appUserRepository = new AppUserRepository(Context);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _appUserRepository.GetAllUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetAllUsersAsync_WithOrderByFirstName_ShouldReturnOrderedUsers()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _appUserRepository.GetAllUsersAsync("firstname", false);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeInAscendingOrder(u => u.FirstName);
        }

        [Fact]
        public async Task GetUSerByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _appUserRepository.GetUSerByIdAsync("test-user-id");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("test-user-id");
        }

        [Fact]
        public async Task GetUSerByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var result = await _appUserRepository.GetUSerByIdAsync("invalid-id");

            // Assert
            result.Should().BeNull();
        }
    }
}