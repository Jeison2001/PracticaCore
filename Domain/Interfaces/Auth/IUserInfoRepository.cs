using Domain.Entities;
using Domain.Interfaces.Registration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces.Auth
{
    public interface IUserInfoRepository : IScopedService
    {
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<List<string>> GetUserPermissionsAsync(int userId);
        Task<User?> FindUserByEmailAsync(string email);
        Task<User> CreateUserIfNotExistsAsync(string email, string firstName, string lastName);
    }
}