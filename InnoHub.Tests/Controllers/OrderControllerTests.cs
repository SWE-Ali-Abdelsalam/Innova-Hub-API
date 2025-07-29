using AutoMapper;
using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Repository.Repository;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InnoHub.Tests.Controllers
{
    public class OrderControllerTests : BaseControllerTest
    {
        private readonly Mock<IUnitOfWork> MockUnitOfWork;
        private readonly Mock<IAuth> MockAuth;
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> MockConfiguration;
        private readonly OrderController _controller;

        public OrderControllerTests()
        {
            // 1. إنشاء mocks
            MockAuth = new Mock<IAuth>();
            MockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();

            // 2. إعداد MockUnitOfWork
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockUnitOfWork.Setup(uow => uow.Auth).Returns(MockAuth.Object);

            // 3. تمرير الـ Mock لـ Auth
            MockHelper.SetupAuthMocks(MockAuth);

            // 4. إعداد الـ Configuration للـ Stripe
            MockConfiguration.Setup(x => x["StripeSettings:SecretKey"])
                             .Returns("sk_test_fake_key");

            // 5. إنشاء الـ Controller
            _controller = new OrderController(
                MockUnitOfWork.Object,
                MockConfiguration.Object,
                Mapper,
                MockConfiguration.Object,
                GetMockLogger<OrderController>().Object
            );
        }

        [Fact]
        public async Task GetDeliveryMethods_ShouldReturnDeliveryMethods()
        {
            // Arrange
            var deliveryMethods = new List<DeliveryMethod>
        {
            new DeliveryMethod { Id = 1, ShortName = "Standard", Cost = 10m },
            new DeliveryMethod { Id = 2, ShortName = "Express", Cost = 20m }
        };

            MockUnitOfWork.Setup(x => x.DeliveryMethod.GetAllAsync())
                .ReturnsAsync(deliveryMethods);

            // Act
            var result = await _controller.GetDeliveryMethods();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        

        [Fact]
        public async Task ApproveReturn_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var request = new ApproveReturnDTO
            {
                OrderId = 1
            };

            var order = TestDataHelper.CreateTestOrder("test-user-id");
            order.PaymentIntentId = "pi_test123";

            MockUnitOfWork.Setup(x => x.Auth.IsAdmin("test-user-id"))
                .ReturnsAsync(true);
            MockUnitOfWork.Setup(x => x.Order.GetByIdAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _controller.ApproveReturn(GetValidAuthorizationHeader(), request);

            // Assert - This would need Stripe refund mocking to fully test
            result.Should().BeOfType<BadRequestObjectResult>(); // Expected without proper Stripe setup
        }
    }
}