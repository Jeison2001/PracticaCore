using Domain.Common;
using Domain.Common.Teachers;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IPreliminaryProjectRepository : IRepository<PreliminaryProject, int>, IScopedService
    {
        Task<PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters, long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false);
        Task<List<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null);
        Task<PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters, long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false);
    }
}
