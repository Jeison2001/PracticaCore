using Domain.Common.SaberPro;
using Domain.Entities;
using Domain.Interfaces.Common;
using Domain.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ISaberProRepository : IRepository<SaberPro, int>, IScopedService
    {
        Task<SaberProWithDetails?> GetWithDetailsAsync(int id);

        Task<PaginatedResult<SaberProWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);

        Task<List<SaberProWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default);
        
        Task<PaginatedResult<SaberProWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);
    }
}
