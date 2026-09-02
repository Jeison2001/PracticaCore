using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    public interface IUserRoleRepository : IScopedService
    {
        Task<PaginatedResult<UserRole>> GetUserRolesWithUserDetailsAsync(
            string? roleCode,
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            Dictionary<string, string>? filters,
            CancellationToken cancellationToken);

        /// <summary>
        /// Obtiene en lote los roles activos con su entidad Role para una colección de IDs de usuario.
        /// </summary>
        Task<List<UserRole>> GetRolesByUserIdsAsync(IEnumerable<int> userIds, CancellationToken ct = default);
    }
}