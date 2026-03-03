using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // For int IDs (Product, Category, Supplier)
        Task<T?> GetByIdAsync(int id);

        // For string IDs (ApplicationUser)
        Task<T?> GetByIdAsync(string id);

        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsAsync(string id);
    }
}