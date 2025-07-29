using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Service.FileService;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;

namespace InnoHub.Tests.Controllers
{
    public class SignatureControllerTests : BaseControllerTest
    {
        private readonly SignatureController _controller;
        private readonly Mock<IAuth> _mockAuth;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<IFileService> _mockFileService;

        public SignatureControllerTests()
        {
            _mockAuth = new Mock<IAuth>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockFileService = new Mock<IFileService>();

            MockUnitOfWork.Setup(x => x.Auth).Returns(_mockAuth.Object);
            MockHelper.SetupAuthMocks(_mockAuth);

            _mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns("/test/wwwroot");

            _controller = new SignatureController(
                MockUnitOfWork.Object,
                _mockWebHostEnvironment.Object,
                GetMockLogger<SignatureController>().Object,
                _mockFileService.Object
            );
        }

        [Fact]
        public async Task UploadSignature_WithValidFile_ShouldReturnOk()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var fileContent = "fake image content";
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("signature.jpg");
            mockFile.Setup(f => f.Length).Returns(fileBytes.Length);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var request = new UploadSignatureDTO
            {
                SignatureImage = mockFile.Object
            };

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.Auth.AuthenticateAndAuthorizeUser(It.IsAny<string>(), "BusinessOwner"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.FileService.EnsureDirectory(It.IsAny<string>()))
                .Returns("wwwroot/SignatureImages");
            MockUnitOfWork.Setup(x => x.FileService.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("/SignatureImages/signature.jpg");

            // Act
            var result = await _controller.UploadSignature(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            MockUnitOfWork.Verify(x => x.Auth.UpdateUser(It.IsAny<AppUser>()), Times.Once);
        }

        [Fact]
        public async Task UploadSignature_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Auth.GetUserIdFromToken(It.IsAny<string>()))
                .Returns((string)null);

            var request = new UploadSignatureDTO();

            // Act
            var result = await _controller.UploadSignature("invalid-token", request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UploadSignature_WithUserNotFound_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync((AppUser)null);

            var request = new UploadSignatureDTO();

            // Act
            var result = await _controller.UploadSignature(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UploadSignature_WithoutBusinessOwnerOrInvestorRole_ShouldReturnUnauthorized()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.Auth.AuthenticateAndAuthorizeUser(It.IsAny<string>(), "BusinessOwner"))
                .ReturnsAsync((AppUser)null);
            MockUnitOfWork.Setup(x => x.Auth.IsInvestor("test-user-id"))
                .ReturnsAsync(false);

            var request = new UploadSignatureDTO();

            // Act
            var result = await _controller.UploadSignature(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UploadSignature_WithoutSignatureImage_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.Auth.AuthenticateAndAuthorizeUser(It.IsAny<string>(), "BusinessOwner"))
                .ReturnsAsync(user);

            var request = new UploadSignatureDTO
            {
                SignatureImage = null
            };

            // Act
            var result = await _controller.UploadSignature(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadSignature_WithInvalidFileExtension_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("signature.txt");
            mockFile.Setup(f => f.Length).Returns(1000);

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.Auth.AuthenticateAndAuthorizeUser(It.IsAny<string>(), "BusinessOwner"))
                .ReturnsAsync(user);

            var request = new UploadSignatureDTO
            {
                SignatureImage = mockFile.Object
            };

            // Act
            var result = await _controller.UploadSignature(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UploadSignature_WithFileTooLarge_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("signature.jpg");
            mockFile.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);
            MockUnitOfWork.Setup(x => x.Auth.AuthenticateAndAuthorizeUser(It.IsAny<string>(), "BusinessOwner"))
                .ReturnsAsync(user);

            var request = new UploadSignatureDTO
            {
                SignatureImage = mockFile.Object
            };

            // Act
            var result = await _controller.UploadSignature(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteSignature_WithExistingSignature_ShouldReturnOk()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            user.SignatureImageUrl = "/SignatureImages/signature.jpg";

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.DeleteSignature(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            MockUnitOfWork.Verify(x => x.Auth.UpdateUser(It.IsAny<AppUser>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSignature_WithoutExistingSignature_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            user.SignatureImageUrl = null;

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.DeleteSignature(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetContract_WithValidDealAndAuthorizedUser_ShouldReturnOk()
        {
            // Arrange
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            deal.ContractDocumentUrl = "/Contracts/contract.pdf";

            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(1))
                .ReturnsAsync(deal);

            // Act
            var result = await _controller.GetContract(GetValidAuthorizationHeader(), 1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetContract_WithDealNotFound_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(999))
                .ReturnsAsync((Deal)null);

            // Act
            var result = await _controller.GetContract(GetValidAuthorizationHeader(), 999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetContract_WithUnauthorizedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var deal = TestDataHelper.CreateTestDeal(1, "other-owner-id", "other-investor-id");

            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(1))
                .ReturnsAsync(deal);
            MockUnitOfWork.Setup(x => x.Auth.IsAdmin("test-user-id"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.GetContract(GetValidAuthorizationHeader(), 1);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetContract_WithNoContractGenerated_ShouldReturnNotFound()
        {
            // Arrange
            var deal = TestDataHelper.CreateTestDeal(1, "test-user-id", "investor-id");
            deal.ContractDocumentUrl = null;

            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(1))
                .ReturnsAsync(deal);

            // Act
            var result = await _controller.GetContract(GetValidAuthorizationHeader(), 1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetContract_WithAdminUser_ShouldReturnOk()
        {
            // Arrange
            var deal = TestDataHelper.CreateTestDeal(1, "other-owner-id", "other-investor-id");
            deal.ContractDocumentUrl = "/Contracts/contract.pdf";

            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(1))
                .ReturnsAsync(deal);
            MockUnitOfWork.Setup(x => x.Auth.IsAdmin("test-user-id"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.GetContract(GetValidAuthorizationHeader(), 1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}