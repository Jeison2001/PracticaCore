using Domain.Common.CoTerminal;
using Domain.Entities;
using Domain.Interfaces.Common;
using Domain.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ICoTerminalRepository : IRepository<CoTerminal, int>, IScopedService
    {
        Task<CoTerminalWithDetails?> GetWithDetailsAsync(int id);
        
        Task<PaginatedResult<CoTerminalWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);

        Task<List<CoTerminalWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default);
        
        Task<PaginatedResult<CoTerminalWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);
    }
}
