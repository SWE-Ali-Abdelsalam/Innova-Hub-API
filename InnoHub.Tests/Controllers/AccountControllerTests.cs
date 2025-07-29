using AutoMapper;
using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Repository.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InnoHub.Tests.Controllers
{
    public class AccountControllerTests : BaseControllerTest
    {
        private readonly Mock<IAuth> _mockAuth;
        private readonly AccountController _controller;
        public AccountControllerTests()
        {
            _mockAuth = new Mock<IAuth>();
            MockUnitOfWork.Setup(x => x.Auth).Returns(_mockAuth.Object);

            _controller = new AccountController(
                MockRoleManager.Object,
                MockUserManager.Object,
                MockUnitOfWork.Object,
                null, // SignInManager - would need proper mock in real scenario
                _mockAuth.Object,
                Mapper,
                GetMockLogger<AccountController>().Object
            );
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            MockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((AppUser)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WithBlockedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var user = TestDataHelper.CreateTestUser();
            user.Isblock = true;

            MockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            MockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var registerDto = new RegisterUserDTO
            {
                Email = "newuser@example.com",
                Password = "NewUser123!",
                FirstName = "New",
                LastName = "User",
                RoleId = "customer-role-id",
                City = "Test City",
                Country = "Test Country",
                PhoneNumber = "1234567890"
            };

            var role = new IdentityRole { Id = "customer-role-id", Name = "Customer" };

            MockRoleManager.Setup(x => x.FindByIdAsync(registerDto.RoleId))
                .ReturnsAsync(role);
            MockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((AppUser)null);
            MockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            MockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), role.Name))
                .ReturnsAsync(IdentityResult.Success);
            _mockAuth.Setup(x => x.CreateToken(It.IsAny<AppUser>(), MockUserManager.Object))
                .ReturnsAsync("fake-jwt-token");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = new RegisterUserDTO
            {
                Email = "existing@example.com",
                RoleId = "customer-role-id"
            };

            var role = new IdentityRole { Id = "customer-role-id", Name = "Customer" };
            var existingUser = TestDataHelper.CreateTestUser();

            MockRoleManager.Setup(x => x.FindByIdAsync(registerDto.RoleId))
                .ReturnsAsync(role);
            MockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
