using InnoHub.Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.Data
{
    public static class RoleDataSeeding
    {
        public static void SeedData(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            SeedRoles(roleManager);
        }

        private static void SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            // Ensure roles are only added once
            CreateRoleIfNotExist(roleManager, "Admin");
            CreateRoleIfNotExist(roleManager, "BusinessOwner");
            CreateRoleIfNotExist(roleManager, "Customer");
            CreateRoleIfNotExist(roleManager, "Investor");
        }

        private static void CreateRoleIfNotExist(RoleManager<IdentityRole> roleManager, string roleName)
        {
            // Check if the role already exists
            if (!roleManager.RoleExistsAsync(roleName).Result)
            {
                // If not, create the role
                var role = new IdentityRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };
                var result = roleManager.CreateAsync(role).Result;
                if (!result.Succeeded)
                {
                    Console.WriteLine($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
