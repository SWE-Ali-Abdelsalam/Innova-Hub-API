using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Core.Models;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Controllers
{
    public class WishlistControllerTests : BaseControllerTest
    {
        private readonly Mock<IUnitOfWork> MockUnitOfWork;
        private readonly Mock<IAuth> MockAuth;
        private readonly WishlistController _controller;

        public WishlistControllerTests()
        {
            // 1. إنشاء الـ Mock الخاص بـ IAuth
            MockAuth = new Mock<IAuth>();

            // 2. إعداد MockUnitOfWork وإرجاع IAuth object من الموك
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockUnitOfWork.Setup(uow => uow.Auth).Returns(MockAuth.Object);

            // 3. تمرير الـ Mock نفسه للدالة
            MockHelper.SetupAuthMocks(MockAuth);

            // 4. إنشاء الـ Controller
            _controller = new WishlistController(MockUnitOfWork.Object);
        }

        [Fact]
        public async Task AddProductToWishlist_WithInvalidProduct_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Product.GetByIdAsync(999))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _controller.AddProductToWishlist(GetValidAuthorizationHeader(), 999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetWishlist_WithValidUser_ShouldReturnWishlist()
        {
            // Arrange
            var wishlist = TestDataHelper.CreateTestWishlist("test-user-id");
            var wishlistItems = new List<WishlistItem>
            {
                new WishlistItem
                {
                    Id = 1,
                    WishlistId = 1,
                    ProductId = 1,
                    Product = TestDataHelper.CreateTestProduct(1)
                }
            };

            MockUnitOfWork.Setup(x => x.Wishlist.GetWishlistByUserID("test-user-id"))
                .ReturnsAsync(wishlist);
            MockUnitOfWork.Setup(x => x.WishlistItem.GetWishlistItemsByWishlistId(1))
                .ReturnsAsync(wishlistItems);

            // Act
            var result = await _controller.GetWishlist(GetValidAuthorizationHeader());

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task RemoveFromWishlist_WithValidProduct_ShouldReturnOk()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Wishlist.RemoveProductFromWishlist(1, "test-user-id"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveFromWishlist(GetValidAuthorizationHeader(), 1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RemoveFromWishlist_WithInvalidProduct_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Wishlist.RemoveProductFromWishlist(999, "test-user-id"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RemoveFromWishlist(GetValidAuthorizationHeader(), 999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}