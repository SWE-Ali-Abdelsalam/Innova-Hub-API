using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Controllers
{
    public class CartControllerTests : BaseControllerTest
    {
        private readonly Mock<IUnitOfWork> MockUnitOfWork;
        private readonly Mock<IAuth> MockAuth;
        private readonly Mock<ICart> MockCart;
        private readonly Mock<IProduct> MockProduct;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            // إنشاء الـ Mocks
            MockAuth = new Mock<IAuth>();
            MockCart = new Mock<ICart>();
            MockProduct = new Mock<IProduct>();

            // إعداد MockUnitOfWork وإرجاع الـ Mocks عند الطلب
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockUnitOfWork.Setup(uow => uow.Auth).Returns(MockAuth.Object);
            MockUnitOfWork.Setup(uow => uow.Cart).Returns(MockCart.Object);
            MockUnitOfWork.Setup(uow => uow.Product).Returns(MockProduct.Object);

            // تم تمرير الـ Mocks إلى الـ helper methods
            MockHelper.SetupAuthMocks(MockAuth);
            MockHelper.SetupCartRepositoryMocks(MockCart);
            MockHelper.SetupProductRepositoryMocks(MockProduct);

            _controller = new CartController(MockUnitOfWork.Object, GetMockLogger<CartController>().Object, Mapper);
        }

        [Fact]
        public async Task AddToCart_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var addToCartDto = new AddToCartDTO
            {
                ProductId = 1,
                Quantity = 2
            };

            // Act
            var result = await _controller.AddToCart(GetValidAuthorizationHeader(), addToCartDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task AddToCart_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Auth.GetUserIdFromToken(It.IsAny<string>()))
                .Returns((string)null);

            var addToCartDto = new AddToCartDTO
            {
                ProductId = 1,
                Quantity = 2
            };

            // Act
            var result = await _controller.AddToCart("invalid-token", addToCartDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UpdateCartItemQuantity_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var updateDto = new UpdateQuantityDTO
            {
                ProductId = 1,
                quantity = 3
            };

            MockUnitOfWork.Setup(x => x.Cart.UpdateProductQuantity("test-user-id", 1, 3))
                .ReturnsAsync(TestDataHelper.CreateTestCart("test-user-id"));

            // Act
            var result = await _controller.UpdateCartItemQuantity(GetValidAuthorizationHeader(), updateDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ClearCart_WithValidUser_ShouldReturnOk()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Cart.ClearCart("test-user-id"))
                .ReturnsAsync(TestDataHelper.CreateTestCart("test-user-id"));

            // Act
            var result = await _controller.ClearCart(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}