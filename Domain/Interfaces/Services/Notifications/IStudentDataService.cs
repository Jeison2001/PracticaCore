using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Notifications;

/// <summary>
/// Extrae datos de estudiantes (nombres, emails, count) desde diferentes orígenes:
/// Proposal, InscriptionModality, o lista de UserIds. Usado por los builders
/// para poblar los placeholders {StudentNames}, {StudentEmails} en los templates.
/// </summary>

public interface IStudentDataService : IScopedService
{
    Task<(string Names, string Emails, int Count)> GetStudentDataByProposalAsync(int proposalId);
    Task<(string Names, string Emails, int Count)> GetStudentDataByInscriptionAsync(int inscriptionId);
    Task<(string Names, string Emails, int Count)> GetStudentDataByUserIdsAsync(IEnumerable<int> userIds);
}
