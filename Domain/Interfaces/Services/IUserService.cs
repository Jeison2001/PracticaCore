using Domain.Interfaces.Common;
using Domain.Common.Users;

namespace Domain.Interfaces.Services
{
    public interface IUserService : IScopedService
    {
        Task<UserIdentificationResult> GetUserIdByIdentification(int idIdentificationType, string identification);
    }
}