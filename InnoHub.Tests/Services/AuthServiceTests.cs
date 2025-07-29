using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Core.Models;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace InnoHub.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Auth _authService;

        public AuthServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserManager = GetMockUserManager();
            _mockRoleManager = GetMockRoleManager();

            // Setup configuration
            _mockConfiguration.Setup(x => x["JWT:SecretKey"]).Returns("YourNewSecure256BitKeyThatIsLongEnoughForSecurity");
            _mockConfiguration.Setup(x => x["JWT:ValidAudience"]).Returns("MySecurityAPIUsers");
            _mockConfiguration.Setup(x => x["JWT:ValidIssuer"]).Returns("https://localhost:7070");
            _mockConfiguration.Setup(x => x["JWT:DurationInDays"]).Returns("7");

            _authService = new Auth(_mockConfiguration.Object, _mockUserManager.Object, _mockRoleManager.Object);
        }

        private Mock<UserManager<AppUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<RoleManager<IdentityRole>> GetMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(store.Object, null, null, null, null);
        }

        [Fact]
        public async Task CreateToken_WithNullUser_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authService.CreateToken(null, _mockUserManager.Object));
        }

        [Fact]
        public async Task CreateToken_WithNullUserManager_ShouldThrowArgumentNullException()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authService.CreateToken(user, null));
        }

        [Fact]
        public void GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var token = CreateTestToken(user.Id);

            // Act
            var userId = _authService.GetUserIdFromToken($"Bearer {token}");

            // Assert
            userId.Should().Be(user.Id);
        }

        [Fact]
        public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
        {
            // Act
            var userId = _authService.GetUserIdFromToken("invalid-token");

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void GetUserIdFromToken_WithNullToken_ShouldReturnNull()
        {
            // Act
            var userId = _authService.GetUserIdFromToken(null);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public async Task GetRoleNameAsync_WithValidUser_ShouldReturnRoleName()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new[] { "Customer", "Admin" });

            // Act
            var roleName = await _authService.GetRoleNameAsync(user);

            // Assert
            roleName.Should().Be("Customer");
        }

        [Fact]
        public async Task GetRoleNameAsync_WithUserWithoutRoles_ShouldReturnNoRole()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new string[0]);

            // Act
            var roleName = await _authService.GetRoleNameAsync(user);

            // Assert
            roleName.Should().Be("No Role");
        }

        [Fact]
        public async Task IsAdmin_WithAdminUser_ShouldReturnTrue()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(true);

            // Act
            var isAdmin = await _authService.IsAdmin(user.Id);

            // Assert
            isAdmin.Should().BeTrue();
        }

        [Fact]
        public async Task IsAdmin_WithNonAdminUser_ShouldReturnFalse()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Act
            var isAdmin = await _authService.IsAdmin(user.Id);

            // Assert
            isAdmin.Should().BeFalse();
        }

        [Fact]
        public async Task IsInvestor_WithInvestorUser_ShouldReturnTrue()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Investor"))
                .ReturnsAsync(true);

            // Act
            var isInvestor = await _authService.IsInvestor(user.Id);

            // Assert
            isInvestor.Should().BeTrue();
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_WithNonExistentRole_ShouldCreateRole()
        {
            // Arrange
            _mockRoleManager.Setup(x => x.RoleExistsAsync("NewRole"))
                .ReturnsAsync(false);
            _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.EnsureRoleExistsAsync("NewRole");

            // Assert
            _mockRoleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == "NewRole")), Times.Once);
        }

        [Fact]
        public async Task EnsureRoleExistsAsync_WithExistingRole_ShouldNotCreateRole()
        {
            // Arrange
            _mockRoleManager.Setup(x => x.RoleExistsAsync("ExistingRole"))
                .ReturnsAsync(true);

            // Act
            await _authService.EnsureRoleExistsAsync("ExistingRole");

            // Assert
            _mockRoleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        private string CreateTestToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes("YourNewSecure256BitKeyThatIsLongEnoughForSecurity");
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", userId),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = "https://localhost:7070",
                Audience = "MySecurityAPIUsers",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}