using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Registration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces.Auth
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
    }
}