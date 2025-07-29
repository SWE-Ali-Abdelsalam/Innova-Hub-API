using InnoHub.Core.Data;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace InnoHub.Tests.Helpers
{
    public static class DatabaseTestHelper
    {
        public static ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static void SeedTestData(ApplicationDbContext context)
        {
            // Seed categories
            context.Categories.AddRange(
                TestDataHelper.CreateTestCategory(1),
                new Category { Id = 2, Name = "Electronics", Description = "Electronic items", IsPopular = false }
            );

            // Seed delivery methods
            context.DeliveryMethods.AddRange(
                new DeliveryMethod { Id = 1, ShortName = "Standard", Description = "Standard Delivery", Cost = 10m, DeliveryTime = "3-5 days" },
                new DeliveryMethod { Id = 2, ShortName = "Express", Description = "Express Delivery", Cost = 20m, DeliveryTime = "1-2 days" }
            );

            context.SaveChanges();
        }
    }
}