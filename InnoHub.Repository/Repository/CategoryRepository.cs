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
    public class CategoryRepository : GenericRepository<Category>, ICategory
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }
       
        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories
                .ToListAsync();
        }

        public async Task<Dictionary<string, string>> GetAuthorNamesByIdsAsync(IEnumerable<string> authorIds)
        {
            return await _context.Users
                .Where(a => authorIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => $"{a.FirstName} {a.LastName}");
        }

    }
}
