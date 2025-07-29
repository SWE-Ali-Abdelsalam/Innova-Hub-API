using InnoHub.Core.Data;
using InnoHub.Core.Models;
using InnoHub.Service.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Repository.Auth
{
    public class Auth : IAuth
    {
        private static long _lastUsedCode = 2333669592; // Starting point for UniqueCode
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        
        public Auth(IConfiguration configuration, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<string> CreateToken(AppUser user, UserManager<AppUser> userManager)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (userManager == null) throw new ArgumentNullException(nameof(userManager));

            // Prepare a list of claims
            var authClaims = new List<Claim>
            {
                new Claim("userId", user.Id), // Add the userId claim here
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id) // Add userId to the claims
            };

            // Fetch user roles and add them to the claims list
            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Generate the security key
            var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

            // Create the token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = _configuration["JWT:ValidAudience"],
                Issuer = _configuration["JWT:ValidIssuer"],
                Expires = DateTime.UtcNow.AddDays(double.Parse(_configuration["JWT:DurationInDays"])),
                Subject = new ClaimsIdentity(authClaims),
                SigningCredentials = new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Return the serialized token
            return tokenHandler.WriteToken(token);
        }
        public string GetUserIdFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length).Trim();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuers = new[] { "https://localhost:7070", "https://innova-hub.premiumasp.net" }, // ✅ دعم كلا الإصدارات
                    ValidAudience = "MySecurityAPIUsers",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourNewSecure256BitKeyThatIsLongEnoughForSecurity"))
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

                return principal?.Claims.FirstOrDefault(c => c.Type == "userId")?.Value; // ✅ استخدم "nameid" بدلًا من ClaimTypes.NameIdentifier
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating token: {ex}");
                return null;
            }
        }
        public List<string> GetRolesFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        }

        public async Task<string> GetRoleNameAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? "No Role";
        }

        public async Task<AppUser> AuthenticateAndAuthorizeUser(string authorizationHeader, string requiredRole)
        {
            var userId = GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !(await _userManager.IsInRoleAsync(user, requiredRole)))
                return null;

            return user;
        }
        
        public async Task<bool> IsAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, "Admin");
        }

        public async Task<bool> IsInvestor(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            return await _userManager.IsInRoleAsync(user, "Investor");
        }

        public async Task<ExternalUserCreationResult> CreateExternalUserAsync(ExternalLoginInfo info, string email)
        {
            var newUser = new AppUser
            {
                Email = email,
                UserName = email.Split("@")[0],
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname),
                IsExternalLogin = true
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                return new ExternalUserCreationResult
                {
                    Success = false,
                    Errors = createResult.Errors.Select(e => e.Description)
                };
            }

            await _userManager.AddLoginAsync(newUser, info);
            await EnsureRoleExistsAsync("Customer");
            await _userManager.AddToRoleAsync(newUser, "Customer");

            var token = await CreateToken(newUser, _userManager);

            return new ExternalUserCreationResult
            {
                Success = true,
                Message = "Account created and assigned the 'Customer' role successfully.",
                UserId = newUser.Id,
                Email = newUser.Email,
                Token = token
            };
        }

        public async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        public async Task<AppUser> GetUserByStripeAccountId(string stripeAccountId)
        {
            return await _userManager.Users.FirstOrDefaultAsync(u => u.StripeAccountId == stripeAccountId);
        }

        public async Task<AppUser> GetUserById(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<bool> UpdateUser(AppUser user)
        {
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<IEnumerable<AppUser>> GetUsersWithPendingIdVerification()
        {
            return await _userManager.Users
                .Where(u =>
                    !string.IsNullOrEmpty(u.IdCardFrontImageUrl) &&
                    !string.IsNullOrEmpty(u.IdCardBackImageUrl))
                .OrderBy(u => u.IdCardUploadDate)
                .ToListAsync();
        }

        public async Task<List<AppUser>> GetUsersByRole(string roleName)
        {
            try
            {
                if (string.IsNullOrEmpty(roleName))
                    return new List<AppUser>();

                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                return usersInRole.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting users by role {roleName}: {ex.Message}");
                return new List<AppUser>();
            }
        }
    }
}
