using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InnoHub.Tests.Controllers
{
    public class ReportControllerTests : BaseControllerTest
    {
        private readonly ReportController _controller;
        private readonly Mock<IAuth> _mockAuth;

        public ReportControllerTests()
        {
            _mockAuth = new Mock<IAuth>();
            MockUnitOfWork.Setup(x => x.Auth).Returns(_mockAuth.Object);
            MockHelper.SetupAuthMocks(_mockAuth);

            _controller = new ReportController(MockUserManager.Object, MockUnitOfWork.Object);
        }

        [Fact]
        public async Task CreateReport_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Auth.GetUserIdFromToken(It.IsAny<string>()))
                .Returns((string)null);

            var createReportDto = new CreateReportDto
            {
                Type = "User",
                TargetId = Guid.NewGuid().ToString(),
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport("invalid-token", createReportDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CreateReport_WithUserNotFound_ShouldReturnUnauthorized()
        {
            // Arrange
            MockUserManager.Setup(x => x.FindByIdAsync("test-user-id"))
                .ReturnsAsync((AppUser)null);

            var createReportDto = new CreateReportDto
            {
                Type = "User",
                TargetId = Guid.NewGuid().ToString(),
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport(GetValidAuthorizationHeader(), createReportDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CreateReport_WithEmptyReportType_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            MockUserManager.Setup(x => x.FindByIdAsync("test-user-id"))
                .ReturnsAsync(user);

            var createReportDto = new CreateReportDto
            {
                Type = "",
                TargetId = "1",
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport(GetValidAuthorizationHeader(), createReportDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateReport_WithInvalidReportType_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            MockUserManager.Setup(x => x.FindByIdAsync("test-user-id"))
                .ReturnsAsync(user);

            var createReportDto = new CreateReportDto
            {
                Type = "InvalidType",
                TargetId = "1",
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport(GetValidAuthorizationHeader(), createReportDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateReport_WithInvalidUserGuid_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            MockUserManager.Setup(x => x.FindByIdAsync("test-user-id"))
                .ReturnsAsync(user);

            var createReportDto = new CreateReportDto
            {
                Type = "User",
                TargetId = "invalid-guid",
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport(GetValidAuthorizationHeader(), createReportDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateReport_WithInvalidDealId_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            MockUserManager.Setup(x => x.FindByIdAsync("test-user-id"))
                .ReturnsAsync(user);

            var createReportDto = new CreateReportDto
            {
                Type = "Deal",
                TargetId = "not-a-number",
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport(GetValidAuthorizationHeader(), createReportDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateReport_WithNonExistentTarget_ShouldReturnNotFound()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            MockUserManager.Setup(x => x.FindByIdAsync("test-user-id"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.Product.GetByIdAsync(999))
                .ReturnsAsync((Product)null);

            var createReportDto = new CreateReportDto
            {
                Type = "Product",
                TargetId = "999",
                Description = "Test report"
            };

            // Act
            var result = await _controller.CreateReport(GetValidAuthorizationHeader(), createReportDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}