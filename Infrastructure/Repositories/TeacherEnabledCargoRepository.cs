using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TeacherEnabledCargoRepository : ITeacherEnabledCargoRepository
    {
        private readonly AppDbContext _context;

        public TeacherEnabledCargoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TeacherEnabledCargo>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Set<TeacherEnabledCargo>()
                .Include(t => t.TypeTeachingAssignment)
                .Where(t => t.IdUser == userId && t.StatusRegister)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<TeacherEnabledCargo>> GetByCargoIdAsync(int cargoId, CancellationToken ct = default)
        {
            return await _context.Set<TeacherEnabledCargo>()
                .Include(t => t.User)
                .Where(t => t.IdTypeTeachingAssignment == cargoId && t.StatusRegister && t.User.StatusRegister)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        /// <summary>
        /// Upsert semántico: activa los cargos solicitados, desactiva los que ya no están
        /// en la lista (StatusRegister = false) y conserva el resto. Idempotente.
        /// </summary>
        public async Task BulkAssignAsync(int userId, IEnumerable<int> typeTeachingAssignmentIds, int? operationUserId, CancellationToken ct = default)
        {
            var requestedIds = typeTeachingAssignmentIds.Distinct().ToList();
            var existing = await _context.Set<TeacherEnabledCargo>()
                .Where(t => t.IdUser == userId)
                .ToListAsync(ct);

            // Desactivar los que ya no están en la lista solicitada
            foreach (var item in existing.Where(e => !requestedIds.Contains(e.IdTypeTeachingAssignment) && e.StatusRegister))
            {
                item.StatusRegister = false;
                item.IdUserUpdatedAt = operationUserId;
                item.UpdatedAt = DateTimeOffset.UtcNow;
                item.OperationRegister = "UPDATE";
            }

            // Insertar los nuevos (idempotente: los ya existentes activos no se duplican)
            var existingActiveIds = existing.Where(e => e.StatusRegister).Select(e => e.IdTypeTeachingAssignment).ToHashSet();
            foreach (var id in requestedIds)
            {
                if (existingActiveIds.Contains(id))
                    continue;

                var inactive = existing.FirstOrDefault(e => e.IdTypeTeachingAssignment == id && !e.StatusRegister);
                if (inactive != null)
                {
                    // Re-activar el registro desactivado (evita reinsertar y reutiliza el Id)
                    inactive.StatusRegister = true;
                    inactive.IdUserUpdatedAt = operationUserId;
                    inactive.UpdatedAt = DateTimeOffset.UtcNow;
                    inactive.OperationRegister = "UPDATE";
                }
                else
                {
                    _context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
                    {
                        IdUser = userId,
                        IdTypeTeachingAssignment = id,
                        IdUserCreatedAt = operationUserId,
                        OperationRegister = "INSERT",
                        StatusRegister = true
                    });
                }
            }
        }
    }
}
