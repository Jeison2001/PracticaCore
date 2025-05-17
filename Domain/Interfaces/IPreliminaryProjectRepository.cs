using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IPreliminaryProjectRepository : IRepository<PreliminaryProject, int>
    {
        Task<List<PreliminaryProject>> GetByUserIdAsync(int userId, bool? status = null);
        Task<List<PreliminaryProject>> GetByTeacherIdAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
        Task<List<PreliminaryProject>> GetAllWithDetailsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters);
    }
}
