using Domain.Interfaces.Registration;

namespace Domain.Interfaces
{
    public interface IUserService : IScopedService
    {
        Task<UserIdentificationResult> GetUserIdByIdentification(int idIdentificationType, string identification);
    }

    public class UserIdentificationResult
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}