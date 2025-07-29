using AutoMapper;
using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using InnoHub.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace InnoHub.Tests.Controllers
{
    public class PaymentControllerTests : BaseControllerTest
    {
        private readonly PaymentController _controller;
        private readonly Mock<IAuth> _mockAuth;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;

        public PaymentControllerTests()
        {
            _mockAuth = new Mock<IAuth>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

            MockUnitOfWork.Setup(x => x.Auth).Returns(_mockAuth.Object);
            MockHelper.SetupAuthMocks(_mockAuth);

            // Setup configuration
            _mockConfiguration.Setup(x => x["StripeSettings:SecretKey"]).Returns("sk_test_fake_key");
            _mockConfiguration.Setup(x => x["StripeSettings:WebhookSecret"]).Returns("whsec_fake_secret");
            _mockConfiguration.Setup(x => x["StripeSettings:PlatformFeePercentage"]).Returns("1.0");
            _mockConfiguration.Setup(x => x["ClientBaseUrl"]).Returns("https://test.com");
            _mockConfiguration.Setup(x => x["MobileSettings:DeepLinkPrefix"]).Returns("testapp://");

            _mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns("/test/wwwroot");

            _controller = new PaymentController(
                MockUnitOfWork.Object,
                _mockConfiguration.Object,
                GetMockLogger<PaymentController>().Object,
                _mockWebHostEnvironment.Object
            );
        }

        [Fact]
        public async Task ProcessWebPayment_WithUnauthorizedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new ProcessPaymentDTO { DealId = 1 };
            var deal = TestDataHelper.CreateTestDeal(1, "owner-id", "other-user-id");

            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(1)).ReturnsAsync(deal);

            // Act
            var result = await _controller.ProcessWebPayment(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task ProcessWebPayment_WithAlreadyProcessedPayment_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new ProcessPaymentDTO { DealId = 1 };
            var deal = TestDataHelper.CreateTestDeal(1, "owner-id", "test-user-id");
            deal.IsPaymentProcessed = true;

            MockUnitOfWork.Setup(x => x.Deal.GetDealWithDetails(1)).ReturnsAsync(deal);

            // Act
            var result = await _controller.ProcessWebPayment(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessProfitPayment_WithNonAdminUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new ProcessProfitPaymentDTO { ProfitDistributionId = 1 };

            _mockAuth.Setup(x => x.IsAdmin("test-user-id")).ReturnsAsync(false);

            // Act
            var result = await _controller.ProcessProfitPayment(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task ConnectStripeAccount_WithNonBusinessOwner_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new ConnectStripeAccountDTO { Platform = "web" };

            MockUnitOfWork.Setup(x => x.Auth.AuthenticateAndAuthorizeUser(It.IsAny<string>(), "BusinessOwner"))
                .ReturnsAsync((AppUser)null);

            // Act
            var result = await _controller.ConnectStripeAccount(GetValidAuthorizationHeader(), request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetStripeAccountStatus_WithValidUser_ShouldReturnStatus()
        {
            // Arrange
            var user = TestDataHelper.CreateTestUser();
            user.StripeAccountId = null; // No connected account

            MockUnitOfWork.Setup(x => x.Auth.GetUserById("test-user-id"))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetStripeAccountStatus(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value;
            response.Should().NotBeNull();
        }

        [Fact]
        public void PaymentCancel_ShouldReturnOk()
        {
            // Act
            var result = _controller.PaymentCancel();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task StripeReturn_WithValidAccountId_ShouldRedirect()
        {
            // Arrange
            var accountId = "acct_test123";
            var user = TestDataHelper.CreateTestUser();

            MockUnitOfWork.Setup(x => x.Auth.GetUserByStripeAccountId(accountId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.StripeReturn(accountId);

            // Assert
            result.Should().BeOfType<RedirectResult>();
        }

        [Fact]
        public void StripeRefresh_ShouldRedirect()
        {
            // Act
            var result = _controller.StripeRefresh();

            // Assert
            result.Should().BeOfType<RedirectResult>();
        }
    }
}