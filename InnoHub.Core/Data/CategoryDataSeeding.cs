using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Data
{
    public static class CategoryDataSeeding
    {
        public static async Task SeedPopularCategories(ApplicationDbContext context)
        {
            var popularCategories = new List<Category>
    {
        new Category { Name = "Home", Description = "", ImageUrl = "/images/categories/Home.png", IsPopular = true },
        new Category { Name = "Bags", Description = "", ImageUrl = "/images/categories/Bags.png", IsPopular = true },
        new Category { Name = "Jewelry", Description = "", ImageUrl = "/images/categories/Jewelry.png", IsPopular = true },
        new Category { Name = "Art", Description = "", ImageUrl = "/images/categories/Art.png", IsPopular = true }
    };

            foreach (var category in popularCategories)
            {
                if (!context.Categories.Any(c => c.Name == category.Name))
                    context.Categories.Add(category);
            }

            await context.SaveChangesAsync();
        }
    }
}
