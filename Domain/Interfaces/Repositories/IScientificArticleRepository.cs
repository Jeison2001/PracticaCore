using Domain.Common.ScientificArticle;
using Domain.Entities;
using Domain.Interfaces.Common;
using Domain.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IScientificArticleRepository : IRepository<ScientificArticle, int>, IScopedService
    {
        Task<ScientificArticleWithDetails?> GetWithDetailsAsync(int id);

        Task<PaginatedResult<ScientificArticleWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);

        Task<List<ScientificArticleWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default);
        
        Task<PaginatedResult<ScientificArticleWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters, 
            CancellationToken cancellationToken = default);
    }
}
