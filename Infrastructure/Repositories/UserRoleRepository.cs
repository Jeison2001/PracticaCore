using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Auth;
using System.Linq.Expressions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Extensions; // required for ToPaginatedResultAsync extension

namespace Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<UserRole>> GetUserRolesWithUserDetailsAsync(
            string? roleCode,
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            Dictionary<string, string>? filters,
            CancellationToken cancellationToken)
        {
            // Crear la consulta base con las relaciones incluidas
            IQueryable<UserRole> query = _context.Set<UserRole>()
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .AsNoTracking();

            // Filtrar por código de rol si se proporciona
            if (!string.IsNullOrEmpty(roleCode))
            {
                string roleCodeLower = roleCode.ToLower();
                query = query.Where(ur => ur.Role.Code.ToLower() == roleCodeLower);
            }

            // Aplicar filtros adicionales
            if (filters != null && filters.Any())
            {
                var additionalFilter = FilterBuilder.BuildFilter<UserRole, int>(filters);
                if (additionalFilter != null)
                {
                    query = query.Where(additionalFilter);
                }
            }

            // Reemplazar lógica de ordenamiento, paginación y construcción de PaginatedResult con ToPaginatedResultAsync
            return await query.ToPaginatedResultAsync<UserRole, int>(
                filters ?? new Dictionary<string, string>(),
                sortBy ?? string.Empty,
                isDescending,
                pageNumber,
                pageSize,
                cancellationToken
            );
        }
    }
}