using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IUserInfoRepository : IScopedService
    {
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<string>> GetUserPermissionsAsync(int userId);
        Task<List<Permission>> GetUserPermissionsFullInfoAsync(int userId);
        Task<User?> FindUserByEmailAsync(string email);
        Task<User> CreateUserIfNotExistsAsync(string email, string firstName, string lastName);
    }
}