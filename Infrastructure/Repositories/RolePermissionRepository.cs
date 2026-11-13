using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Implementación EF Core de <see cref="IRolePermissionRepository"/>.
    /// </summary>
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly AppDbContext _context;

        public RolePermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken ct = default)
        {
            return await _context.Set<Role>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);
        }

        public async Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken ct = default)
        {
            var normalizedCode = roleCode.ToLower();
            return await _context.Set<Role>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Code.ToLower() == normalizedCode, ct);
        }

        public async Task<List<RolePermission>> GetByRoleIdWithPermissionsAsync(
            int roleId,
            bool? statusRegister,
            CancellationToken ct = default)
        {
            IQueryable<RolePermission> query = _context.Set<RolePermission>()
                .AsNoTracking()
                .Include(rp => rp.Permission)
                .Where(rp => rp.IdRole == roleId && rp.Permission != null);

            if (statusRegister.HasValue)
            {
                query = query.Where(rp => rp.StatusRegister == statusRegister.Value);
            }

            return await query
                .OrderBy(rp => rp.Permission.Code)
                .ToListAsync(ct);
        }
    }
}
