using Domain.Common.Seminar;
using Domain.Entities;
using Domain.Interfaces.Common;
using Domain.Common;

namespace Domain.Interfaces.Repositories
{
    public interface ISeminarRepository : IRepository<Seminar, int>, IScopedService
    {
        Task<SeminarWithDetails?> GetWithDetailsAsync(int id);

        /// <summary>
        /// Paginación keyset opcional: con cursor (cursorCreatedAt, cursorId) la paginación es
        /// por cursor (CreatedAt DESC, Id DESC) con coste constante en cualquier profundidad;
        /// skipTotalCount omite el COUNT (TotalRecords = -1). Sin cursor: OFFSET + COUNT clásico.
        /// </summary>
        Task<PaginatedResult<SeminarWithDetails>> GetAllWithDetailsPaginatedAsync(
            int pageNumber,
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false);

        Task<List<SeminarWithDetails>> GetByUserAsync(int userId, bool? status = null, CancellationToken cancellationToken = default);

        Task<PaginatedResult<SeminarWithDetails>> GetByTeacherAsync(
            int teacherId,
            int pageNumber,
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false);
    }
}
