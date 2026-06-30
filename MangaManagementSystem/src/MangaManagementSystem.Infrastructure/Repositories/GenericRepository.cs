using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

        public virtual async Task<T?> GetByIdAsync(object id)
        {
            if (id is object[] keys)
            {
                var entry = await _dbSet.FindAsync(keys);
                return entry;
            }

            var single = await _dbSet.FindAsync(id);
            return single;
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public virtual async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public virtual void Update(T entity)
            => _dbSet.Update(entity);

        public virtual void Delete(T entity)
            => _dbSet.Remove(entity);

        public virtual async Task<Dictionary<TKey, int>> CountByAsync<TKey>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> keySelector) where TKey : notnull
            => await _dbSet.Where(predicate)
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

        public virtual async Task<int> ExecuteDeleteAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ExecuteDeleteAsync();
    }
}
