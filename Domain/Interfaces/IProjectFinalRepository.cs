using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IProjectFinalRepository : IRepository<ProjectFinal, int>
    {
        Task<List<ProjectFinal>> GetByUserIdAsync(int userId, bool? status = null);
        Task<List<ProjectFinal>> GetByTeacherIdAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
        Task<List<ProjectFinal>> GetAllWithDetailsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
    }
}
