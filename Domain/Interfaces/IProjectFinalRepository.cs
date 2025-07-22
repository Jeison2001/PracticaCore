using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Registration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IProjectFinalRepository : IRepository<ProjectFinal, int>, IScopedService
    {
        Task<PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
        Task<List<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null);
        Task<PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
    }
}
