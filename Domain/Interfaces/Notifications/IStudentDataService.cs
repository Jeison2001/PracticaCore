using Domain.Interfaces.Registration;

namespace Domain.Interfaces.Notifications
{
    public interface IStudentDataService : IScopedService
    {
        Task<(string Names, string Emails, int Count)> GetStudentDataByProposalAsync(int proposalId);
        Task<(string Names, string Emails, int Count)> GetStudentDataByInscriptionAsync(int inscriptionId);
        Task<(string Names, string Emails, int Count)> GetStudentDataByUserIdsAsync(IEnumerable<int> userIds);
    }
}
