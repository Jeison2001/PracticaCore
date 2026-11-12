using Domain.Common;
using Domain.Common.Teachers;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IProjectPresentationRepository : IRepository<ProjectPresentation, int>, IScopedService
    {
        Task<PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters, long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false);
        Task<List<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null);
        Task<PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters, long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false);
    }
}
