using InnoHub.Core.Data;
using InnoHub.Core.Models;
using InnoHub.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace InnoHub.Tests.BaseTests
{
    public abstract class BaseRepositoryTest : IDisposable
    {
        protected ApplicationDbContext Context { get; }

        protected BaseRepositoryTest()
        {
            Context = DatabaseTestHelper.GetInMemoryDbContext();
        }

        protected async Task SeedTestDataAsync()
        {
            // Seed test users
            var testUser = TestDataHelper.CreateTestUser("test-user-id", "test@example.com");
            var ownerUser = TestDataHelper.CreateTestUser("owner-id", "owner@example.com");
            var investorUser = TestDataHelper.CreateTestUser("investor-id", "investor@example.com");

            Context.Users.AddRange(testUser, ownerUser, investorUser);

            // Seed categories
            var category1 = TestDataHelper.CreateTestCategory(1);
            var category2 = new Category
            {
                Id = 2,
                Name = "Electronics",
                Description = "Electronic items",
                IsPopular = false,
                ImageUrl = "/test2.jpg"
            };
            Context.Categories.AddRange(category1, category2);

            // Seed delivery methods
            var deliveryMethod1 = new DeliveryMethod
            {
                Id = 1,
                ShortName = "Standard",
                Description = "Standard Delivery",
                Cost = 10m,
                DeliveryTime = "3-5 days"
            };
            var deliveryMethod2 = new DeliveryMethod
            {
                Id = 2,
                ShortName = "Express",
                Description = "Express Delivery",
                Cost = 20m,
                DeliveryTime = "1-2 days"
            };
            Context.DeliveryMethods.AddRange(deliveryMethod1, deliveryMethod2);

            await Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}