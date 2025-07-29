using Ecommerce_platforms.Repository.Auth;
using FluentAssertions;
using InnoHub.Controllers;
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
    public class CategoryControllerTests : BaseControllerTest
    {
        private readonly Mock<IUnitOfWork> MockUnitOfWork;
        private readonly Mock<IAuth> MockAuth;
        private readonly Mock<UserManager<AppUser>> MockUserManager;
        private readonly CategoryController _controller;

        public CategoryControllerTests()
        {
            // 1. إنشاء mocks
            MockAuth = new Mock<IAuth>();
            MockUserManager = GetMockUserManager();

            // 2. إعداد MockUnitOfWork
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockUnitOfWork.Setup(uow => uow.Auth).Returns(MockAuth.Object);

            // 3. تمرير الـ Mock نفسه (مش الـ Object)
            MockHelper.SetupAuthMocks(MockAuth);

            // 4. إنشاء الكونترولر
            _controller = new CategoryController(MockUnitOfWork.Object, MockUserManager.Object);
        }

        [Fact]
        public async Task GetAllCategories_ShouldReturnAllCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                TestDataHelper.CreateTestCategory(1),
                TestDataHelper.CreateTestCategory(2)
            };

            MockUnitOfWork.Setup(x => x.Category.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetAllCategories();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var categoryList = okResult.Value as List<CategoryViewModel>;
            categoryList.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetCategoryById_WithValidId_ShouldReturnCategory()
        {
            // Arrange
            var category = TestDataHelper.CreateTestCategory(1);
            MockUnitOfWork.Setup(x => x.Category.GetByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.GetCategoryById(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCategoryById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            MockUnitOfWork.Setup(x => x.Category.GetByIdAsync(999))
                .ReturnsAsync((Category)null);

            // Act
            var result = await _controller.GetCategoryById(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetPopularCategories_ShouldReturnPopularCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Popular Category", IsPopular = true, ImageUrl = "/test.jpg" },
                new Category { Id = 2, Name = "Regular Category", IsPopular = false, ImageUrl = "/test2.jpg" }
            };

            MockUnitOfWork.Setup(x => x.Category.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetPopularCategories();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().NotBeNull();
        }
    }
}