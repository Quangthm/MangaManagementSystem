using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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

        public virtual async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public virtual void Update(T entity)
            => _dbSet.Update(entity);

        public virtual void Delete(T entity)
            => _dbSet.Remove(entity);
    }
}
