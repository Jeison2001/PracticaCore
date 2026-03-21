using Domain.Entities;
using Domain.Interfaces.Common;

namespace Application.Common.Services.Jobs
{
    public interface INotificationBackgroundJob : ITransientService
    {
        Task HandleEntityCreationAsync<T, TId>(TId id) where T : BaseEntity<TId> where TId : struct;
        Task HandleProposalChangeAsync(int proposalId, int oldStateId);
        Task HandleTeachingAssignmentChangeAsync(int assignmentId, int oldTeacherId);
        Task HandleAcademicPracticeChangeAsync(int practiceId, int oldStateId);
    }
}
