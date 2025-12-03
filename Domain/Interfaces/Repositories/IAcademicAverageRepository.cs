using Domain.Common.AcademicAverage;
using Domain.Entities;
using Domain.Interfaces.Common;
using Domain.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IAcademicAverageRepository : IRepository<AcademicAverage, int>, IScopedService
    {
        Task<AcademicAverageWithDetails?> GetWithDetailsAsync(int id);

        Task<PaginatedResult<AcademicAverageWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);

        Task<List<AcademicAverageWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default);
        
        Task<PaginatedResult<AcademicAverageWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);
    }
}
