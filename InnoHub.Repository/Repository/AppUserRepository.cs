using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class AppUserRepository : GenericRepository<AppUser>, IAppUser
    {
        public AppUserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<AppUser>> GetAllUsersAsync(string orderBy = "Id", bool descending = false)
        {
            IQueryable<AppUser> query = _context.Users;

            query = orderBy.ToLower() switch
            {
                "firstname" => descending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
                "lastname" => descending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
                "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                _ => descending ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id),
            };

            return await query.ToListAsync();
        }

        public async Task<AppUser> GetUSerByIdAsync(string userID)
        {
            return await _context.Users.FirstOrDefaultAsync(a => a.Id == userID);
        }

        public async Task<int> GetBusinessOwnerFollowersCountAsync(string sellerId)
        {
            // 1. المستخدمين الذين قيموا منتجاته
            var userIdsFromRatings = await _context.Products
                .Where(p => p.AuthorId == sellerId)
                .SelectMany(p => p.Ratings.Select(r => r.UserId))
                .Distinct()
                .ToListAsync();

            // 2. المستخدمين الذين اشتروا منتجاته
            var userIdsFromOrders = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.AuthorId == sellerId))
                .Select(o => o.UserId)
                .Distinct()
                .ToListAsync();

            // 3. المستخدمين الذين استثمروا في صفقاته
            var userIdsFromDeals = await _context.Deals
                .Where(d => d.AuthorId == sellerId && d.InvestorId != null)
                .Select(d => d.InvestorId)
                .Distinct()
                .ToListAsync();

            // دمج كل المستخدمين وإزالة التكرار
            var allUniqueFollowers = userIdsFromRatings
                .Union(userIdsFromOrders)
                .Union(userIdsFromDeals)
                .Distinct()
                .Count();

            return allUniqueFollowers;
        }
    }
}
