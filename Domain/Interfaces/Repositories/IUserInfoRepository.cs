using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IUserInfoRepository : IScopedService
    {
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<PermissionWithUserPermission>> GetUserPermissionsWithUserPermissionsAsync(int userId);
        Task<UserLoginData> GetUserLoginDataAsync(int userId);
        Task<User?> FindUserByEmailAsync(string email);
        Task<User?> FindUserByIdentificationAsync(string identification);
        Task<User> CreateUserIfNotExistsAsync(string email, string firstName, string lastName);
    }
}