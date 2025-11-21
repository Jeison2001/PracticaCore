using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IPreliminaryProjectRepository : IRepository<PreliminaryProject, int>, IScopedService
    {
        Task<PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
        Task<List<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null);
        Task<PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
    }
}
