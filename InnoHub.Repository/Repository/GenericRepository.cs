using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Repository.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // Find the entity by its ID
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
            {
                // Return false if the entity does not exist
                return false;
            }

            // Remove the entity from the context
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();

            // Return true indicating the entity was successfully deleted
            return true;
        }


        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }
        public async Task<T> UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<T>> GetPaginatedAsync(
    int pageNumber,
    int pageSize,
    string orderBy = "Id",
    bool descending = true,
    List<Expression<Func<T, object>>>? includes = null,
    Expression<Func<T, bool>>? filter = null) // Added filter parameter
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include); // Include related entities
                }
            }

            if (filter != null)
            {
                query = query.Where(filter); // Apply filter if provided
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, orderBy);
            var lambda = Expression.Lambda(property, parameter);

            query = descending
                ? Queryable.OrderByDescending(query, (dynamic)lambda)
                : Queryable.OrderBy(query, (dynamic)lambda);

            return await query
                .Skip((pageNumber - 1) * pageSize) // Apply pagination
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> filter = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
            {
                query = query.Where(filter); // Apply filter if provided
            }

            return await query.CountAsync();
        }


    }
}
