using Domain.Entities;
using Domain.Interfaces.Common;

namespace Application.Common.Services.Jobs
{
    public interface INotificationBackgroundJob : ITransientService
    {
        Task HandleEntityCreationAsync<T, TId>(TId id) where T : BaseEntity<TId> where TId : struct;
        Task HandleProposalCreationAsync(int proposalId);
        Task HandleProposalChangeAsync(int proposalId, int oldStateId);
        Task HandleTeachingAssignmentCreationAsync(int assignmentId);
        Task HandleTeachingAssignmentChangeAsync(int assignmentId, int oldTeacherId);
        Task HandleAcademicPracticeChangeAsync(int practiceId, int oldStateId);
        Task HandlePreliminaryProjectChangeAsync(int preliminaryId, int oldStateId);
        Task HandleProjectFinalChangeAsync(int projectFinalId, int oldStateId);
        Task HandleInscriptionCreationAsync(int inscriptionId);
        Task HandleInscriptionChangeAsync(int inscriptionId, int oldStateId);
    }
}
