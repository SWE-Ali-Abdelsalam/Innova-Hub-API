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
    public class ProfileControllerTests : BaseControllerTest
    {
        private readonly Mock<IAuth> _mockAuth;
        private readonly Mock<InnoHub.Service.OtpService> _mockOtpService;
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            _mockAuth = new Mock<IAuth>();
            _mockOtpService = new Mock<InnoHub.Service.OtpService>();
            MockUnitOfWork.Setup(x => x.Auth).Returns(_mockAuth.Object);

            _controller = new ProfileController(
                _mockAuth.Object,
                MockUserManager.Object,
                MockRoleManager.Object,
                null, // EmailSender
                MockUnitOfWork.Object,
                GetMockLogger<ProfileController>().Object,
                _mockOtpService.Object
            );
        }

        [Fact]
        public async Task GetProfile_WithValidToken_ShouldReturnProfile()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var role = new IdentityRole { Id = "role-id", Name = "Customer" };

            _mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns(user.Id);
            MockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            MockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new[] { "Customer" });
            MockRoleManager.Setup(x => x.FindByNameAsync("Customer"))
                .ReturnsAsync(role);

            // Act
            var result = await _controller.GetProfile(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetProfile_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns((string)null);

            // Act
            var result = await _controller.GetProfile("invalid-token");

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UpdateProfile_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var updateDto = new UpdateProfileDTO
            {
                FirstName = "Updated",
                LastName = "Name",
                City = "New City"
            };

            _mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns(user.Id);
            MockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            MockUserManager.Setup(x => x.UpdateAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.UpdateProfile(GetValidAuthorizationHeader(), updateDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ChangePassword_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var updatePasswordDto = new UpdatePasswordDTO
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            _mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns(user.Id);
            MockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            MockUserManager.Setup(x => x.ChangePasswordAsync(user, updatePasswordDto.CurrentPassword, updatePasswordDto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ChangePassword(GetValidAuthorizationHeader(), updatePasswordDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteAccount_WithValidPassword_ShouldReturnOk()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var deleteDto = new DeleteAccountDTO
            {
                Password = "ValidPassword123!"
            };

            _mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns(user.Id);
            MockUserManager.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);
            MockUserManager.Setup(x => x.CheckPasswordAsync(user, deleteDto.Password))
                .ReturnsAsync(true);
            MockUserManager.Setup(x => x.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteAccount(GetValidAuthorizationHeader(), deleteDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetOrders_WithValidUser_ShouldReturnOrders()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var orders = new[] { TestDataHelper.CreateTestOrder(user.Id) };

            _mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns(user.Id);
            MockUnitOfWork.Setup(x => x.Order.GetAllOrdersForSpecificUser(user.Id))
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetOrders(GetValidAuthorizationHeader(), 1, 5);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
