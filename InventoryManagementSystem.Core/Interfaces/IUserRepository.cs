using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;

namespace InventoryManagementSystem.Core.Interfaces
{
    public interface IUserRepository : IRepository<ApplicationUser>
    {
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<ApplicationUser?> GetByUserNameAsync(string userName);
    }
}