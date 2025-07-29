using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IAppUser :IGenericRepository<AppUser>
    {
        public Task<AppUser> GetUSerByIdAsync(string userID);
        
        Task<List<AppUser>> GetAllUsersAsync(string orderBy = "Id", bool descending = false);

        Task<int> GetBusinessOwnerFollowersCountAsync(string sellerId);
    }
}
