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
    public class ShippingAddressControllerTests : BaseControllerTest
    {
        private readonly ShippingAddressController _controller;
        private readonly Mock<IAuth> _mockAuth;

        public ShippingAddressControllerTests()
        {
            _mockAuth = new Mock<IAuth>();

            // Assuming MockUnitOfWork is a Mock<IUnitOfWork>
            MockUnitOfWork.Setup(u => u.Auth).Returns(_mockAuth.Object);

            MockHelper.SetupAuthMocks(_mockAuth);

            _controller = new ShippingAddressController(
                MockUnitOfWork.Object,
                Mapper
            );
        }

        [Fact]
        public async Task AddShippingAddress_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var addressDto = new ShippingAddressDTO
            {
                FirstName = "John",
                LastName = "Doe",
                StreetAddress = "123 Main St",
                City = "Test City",
                ZipCode = "12345",
                Email = "john@example.com",
                Phone = "1234567890"
            };

            MockUnitOfWork.Setup(x => x.shippingAddress.GetShippingAddressByUserId("test-user-id"))
                .ReturnsAsync((ShippingAddress)null);

            // Act
            var result = await _controller.AddShippingAddress(GetValidAuthorizationHeader(), addressDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task AddShippingAddress_WithExistingAddress_ShouldReturnBadRequest()
        {
            // Arrange
            var addressDto = new ShippingAddressDTO
            {
                FirstName = "John",
                LastName = "Doe"
            };

            var existingAddress = TestDataHelper.CreateTestShippingAddress("test-user-id");

            MockUnitOfWork.Setup(x => x.shippingAddress.GetShippingAddressByUserId("test-user-id"))
                .ReturnsAsync(existingAddress);

            // Act
            var result = await _controller.AddShippingAddress(GetValidAuthorizationHeader(), addressDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateShippingAddress_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var addressDto = new ShippingAddressDTO
            {
                FirstName = "Updated",
                LastName = "Name",
                StreetAddress = "456 Updated St",
                City = "Updated City",
                ZipCode = "54321",
                Email = "updated@example.com",
                Phone = "0987654321"
            };

            var existingAddress = TestDataHelper.CreateTestShippingAddress("test-user-id");

            MockUnitOfWork.Setup(x => x.shippingAddress.GetShippingAddressByUserId("test-user-id"))
                .ReturnsAsync(existingAddress);

            // Act
            var result = await _controller.UpdateShippingAddress(GetValidAuthorizationHeader(), addressDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetShippingAddress_WithExistingAddress_ShouldReturnAddress()
        {
            // Arrange
            var shippingAddress = TestDataHelper.CreateTestShippingAddress("test-user-id");

            MockUnitOfWork.Setup(x => x.shippingAddress.GetShippingAddressByUserId("test-user-id"))
                .ReturnsAsync(shippingAddress);

            // Act
            var result = await _controller.GetShippingAddress(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetShippingAddress_WithNoAddress_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.shippingAddress.GetShippingAddressByUserId("test-user-id"))
                .ReturnsAsync((ShippingAddress)null);

            // Act
            var result = await _controller.GetShippingAddress(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CheckShippingAddress_WithExistingAddress_ShouldReturnTrue()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.shippingAddress.UserHasShippingAddress("test-user-id"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckShippingAddress(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value;
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task GetOrUpdateOrderSummary_WithBuyNowProduct_ShouldReturnSummary()
        {
            // Arrange
            var product = TestDataHelper.CreateTestProduct(1);
            var request = new UpdateShippingDTO
            {
                DeliveryMethodId = 1
            };

            var deliveryMethod = new DeliveryMethod { Id = 1, Cost = 10m };

            MockUnitOfWork.Setup(x => x.Product.GetByIdAsync(1))
                .ReturnsAsync(product);
            MockUnitOfWork.Setup(x => x.DeliveryMethod.GetByIdAsync(1))
                .ReturnsAsync(deliveryMethod);

            // Act
            var result = await _controller.GetOrUpdateOrderSummary(
                GetValidAuthorizationHeader(), 1, 2, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
