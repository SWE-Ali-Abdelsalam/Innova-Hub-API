using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface ICategory : IGenericRepository<Category>
    {
        public Task<Dictionary<string, string>> GetAuthorNamesByIdsAsync(IEnumerable<string> authorIds);
        public Task<IEnumerable<Category>> GetAllAsync();
    }
}
