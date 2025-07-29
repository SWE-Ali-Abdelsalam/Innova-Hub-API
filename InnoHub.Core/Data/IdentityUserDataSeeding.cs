using InnoHub.Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Data
{
    public static class IdentityUserDataSeeding
    {
        public static async Task SeedUserAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        { 
            // seed roles
            if (!roleManager.Roles.Any())
            {
                var roles = new List<IdentityRole>
    {
        new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
        new IdentityRole { Name = "BusinessOwner", NormalizedName = "BUSINESSOWNER" },
        new IdentityRole { Name = "Customer", NormalizedName = "CUSTOMER" },
        new IdentityRole { Name = "Investor", NormalizedName = "INVESTOR" }
    };
                foreach (var role in roles)
                {
                    var result = await roleManager.CreateAsync(role);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create role {role.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Seed Users
            if (!userManager.Users.Any())
            {
                var user = new AppUser
                {
                    FirstName = "Admin",
                    LastName = "Admin",
                    UserName = "MohamedSamir",
                    Email = "Admin@gmail.com",
                    City = "Cairo",
                    Country = "Egypt",
                    District = "Nasr City",
                    PhoneNumber = "1234567890",
                    IsStripeAccountEnabled = false
                };

                var result = await userManager.CreateAsync(user, "Admin@@22");
                if (result.Succeeded)
                {
                    // Assign roles to the user
                    var roleResult = await userManager.AddToRoleAsync(user, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        throw new Exception($"Failed to assign role to user: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

    }
}

