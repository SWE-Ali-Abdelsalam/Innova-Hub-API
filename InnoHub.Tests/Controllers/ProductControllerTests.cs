using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InnoHub.Tests.Controllers
{
    public class ProductControllerTests : BaseControllerTest
    {
        private readonly Mock<IUnitOfWork> MockUnitOfWork;
        private readonly Mock<IAuth> MockAuth;
        private readonly Mock<IProduct> MockProduct;
        private readonly Mock<UserManager<AppUser>> MockUserManager;
        private readonly ProductController _controller;

        public ProductControllerTests()
        {
            // 1. إنشاء mocks
            MockAuth = new Mock<IAuth>();
            MockProduct = new Mock<IProduct>();
            MockUserManager = GetMockUserManager(); // هنتكلم عنها بعد شوية

            // 2. إعداد MockUnitOfWork ليرجع mocks دي
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockUnitOfWork.Setup(uow => uow.Auth).Returns(MockAuth.Object);
            MockUnitOfWork.Setup(uow => uow.Product).Returns(MockProduct.Object);

            // 3. تمرير الـ mocks نفسها مش الـ Object
            MockHelper.SetupAuthMocks(MockAuth);
            MockHelper.SetupProductRepositoryMocks(MockProduct);

            // 4. إنشاء الكونترولر
            _controller = new ProductController(MockUnitOfWork.Object, Mapper, MockUserManager.Object);
        }

        //private Mock<UserManager<AppUser>> GetMockUserManager()
        //{
        //    var store = new Mock<IUserStore<AppUser>>();
        //    return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        //}

        [Fact]
        public async Task GetAllProducts_ShouldReturnPaginatedProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                TestDataHelper.CreateTestProduct(1),
                TestDataHelper.CreateTestProduct(2)
            };

            MockUnitOfWork.Setup(x => x.Product.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
                .ReturnsAsync(2);

            MockUnitOfWork.Setup(x => x.Product.GetPaginatedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<List<System.Linq.Expressions.Expression<System.Func<Product, object>>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
                .ReturnsAsync(products);

            // Act
            var result = await _controller.GetAllProducts(1, 10);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetProductById_WithInvalidId_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetProductById(0);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetProductById_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Product.GetByIdAsync(999))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _controller.GetProductById(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetAllProductComments_WithValidProductId_ShouldReturnComments()
        {
            // Arrange
            var product = TestDataHelper.CreateTestProduct(1);
            var comments = new List<ProductComment>
            {
                new ProductComment
                {
                    Id = 1,
                    ProductId = 1,
                    UserId = "test-user-id",
                    CommentText = "Great product!",
                    User = TestDataHelper.CreateTestUser("test-user-id")
                }
            };

            MockUnitOfWork.Setup(x => x.Product.GetByIdAsync(1))
                .ReturnsAsync(product);
            MockUnitOfWork.Setup(x => x.ProductComment.GetCommentsByProductIdAsync(1))
                .ReturnsAsync(comments);

            // Act
            var result = await _controller.GetAllProductComments(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetBestSellingProducts_ShouldReturnTopProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                TestDataHelper.CreateTestProduct(1),
                TestDataHelper.CreateTestProduct(2)
            };

            MockUnitOfWork.Setup(x => x.Product.GetBestSellingProductsAsync(10))
                .ReturnsAsync(products);

            // Act
            var result = await _controller.GetBestSellingProducts(10);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }
    }
}