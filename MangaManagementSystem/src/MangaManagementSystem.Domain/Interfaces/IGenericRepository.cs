using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);

        Task<IReadOnlyList<T>> FindTrackedAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Grouped COUNT executed in the database (one query). Lets callers learn row
        /// counts per key without materializing the rows.
        /// </summary>
        Task<Dictionary<TKey, int>> CountByAsync<TKey>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> keySelector) where TKey : notnull;

        /// <summary>
        /// Set-based DELETE executed directly in the database (no entity materialization).
        /// Returns the number of rows deleted.
        /// </summary>
        Task<int> ExecuteDeleteAsync(Expression<Func<T, bool>> predicate);
    }
}
