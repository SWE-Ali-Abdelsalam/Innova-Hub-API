using InnoHub.Core.Models;
using InnoHub.Service.Auth;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Ecommerce_platforms.Repository.Auth
{
    public interface IAuth
    {
        
        public Task EnsureRoleExistsAsync(string roleName);
        public Task<ExternalUserCreationResult> CreateExternalUserAsync(ExternalLoginInfo info, string email);
        public List<string> GetRolesFromToken(string token);
        public Task<string> GetRoleNameAsync(AppUser user);
        public Task<bool> IsAdmin(string userId);
        Task<bool> IsInvestor(string userId);
        public Task<AppUser> AuthenticateAndAuthorizeUser(string authorizationHeader, string requiredRole);
        public string GetUserIdFromToken(string token);
        Task<string> CreateToken(AppUser user, Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager);
        Task<AppUser> GetUserByStripeAccountId(string stripeAccountId);
        Task<AppUser> GetUserById(string userId);
        Task<bool> UpdateUser(AppUser user);
        Task<IEnumerable<AppUser>> GetUsersWithPendingIdVerification();
        Task<List<AppUser>> GetUsersByRole(string roleName);
    }
}
