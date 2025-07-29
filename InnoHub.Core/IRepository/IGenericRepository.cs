using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Core.IRepository
{
    public interface IGenericRepository<T> where T : class
    {
        public Task<T> AddAsync(T entity);
        public Task<T> UpdateAsync(T entity);
        public Task<bool> DeleteAsync(int id);
        public Task<IEnumerable<T>> GetAllAsync();
        public Task<T> GetByIdAsync(int id);
        public  Task<IEnumerable<T>> GetPaginatedAsync(
    int pageNumber,
    int pageSize,
    string orderBy = "Id",
    bool descending = true,
    List<Expression<Func<T, object>>>? includes = null,
    Expression<Func<T, bool>>? filter = null);

        public Task<int> CountAsync(Expression<Func<T, bool>> filter = null);

    }
}
