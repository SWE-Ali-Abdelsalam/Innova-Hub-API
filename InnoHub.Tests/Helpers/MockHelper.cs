using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Tests.Helpers
{
    public static class MockHelper
    {
        public static void SetupAuthMocks(Mock<IAuth> mockAuth, string userId = "test-user-id")
        {
            mockAuth.Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
                .Returns(userId);

            mockAuth.Setup(x => x.IsAdmin(userId))
                .ReturnsAsync(false);

            mockAuth.Setup(x => x.IsInvestor(userId))
                .ReturnsAsync(false);

            mockAuth.Setup(x => x.GetUserById(userId))
                .ReturnsAsync(TestDataHelper.CreateTestUser(userId));
        }

        public static void SetupProductRepositoryMocks(Mock<IProduct> mockProductRepo)
        {
            var products = new List<Product>
            {
                TestDataHelper.CreateTestProduct(1),
                TestDataHelper.CreateTestProduct(2)
            };

            mockProductRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);

            mockProductRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => products.FirstOrDefault(p => p.Id == id));

            mockProductRepo.Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
                .ReturnsAsync(products.Count);
        }

        public static void SetupCartRepositoryMocks(Mock<ICart> mockCartRepo, string userId = "test-user-id")
        {
            var cart = TestDataHelper.CreateTestCart(userId);

            mockCartRepo.Setup(x => x.GetCartBYUserId(userId))
                .ReturnsAsync(cart);

            mockCartRepo.Setup(x => x.CreateCart(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(cart);
        }

        public static void SetupDealRepositoryMocks(Mock<IDeal> mockDealRepo)
        {
            var deals = new List<Deal>
            {
                TestDataHelper.CreateTestDeal(1),
                TestDataHelper.CreateTestDeal(2)
            };

            mockDealRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(deals);

            mockDealRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => deals.FirstOrDefault(d => d.Id == id));

            mockDealRepo.Setup(x => x.GetDealWithDetails(It.IsAny<int>()))
                .ReturnsAsync((int id) => deals.FirstOrDefault(d => d.Id == id));
        }
    }
}