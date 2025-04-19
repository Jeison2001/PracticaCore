namespace Domain.Interfaces
{
    public interface IUserService
    {
        Task<UserIdentificationResult> GetUserIdByIdentification(int idIdentificationType, string identification);
    }

    public class UserIdentificationResult
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }
}