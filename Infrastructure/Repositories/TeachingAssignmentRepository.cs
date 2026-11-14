using System.Collections.Concurrent;
using Domain.Common.TeachingAssignments;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{    public class TeachingAssignmentRepository : BaseRepository<TeachingAssignment, int>, ITeachingAssignmentRepository
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> AssignmentLocks = new();
        private static readonly ConcurrentDictionary<(int TeacherId, int TypeId), SemaphoreSlim> QuotaLocks = new();

        public TeachingAssignmentRepository(AppDbContext context) : base(context)
        {
        }        public async Task<List<TeachingAssignmentWithDetails>> GetTeachingAssignmentsByProposalWithDetailsAsync(
            int proposalId, 
            bool? statusRegister = null, 
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<TeachingAssignment>()
                .AsNoTracking()
                .Include(ta => ta.Teacher)
                .Include(ta => ta.TypeTeachingAssignment)
                .Where(ta => ta.IdInscriptionModality == proposalId);
                  
            // Aplicar filtro por estado si se proporciona
            if (statusRegister.HasValue)
            {
                query = query.Where(ta => ta.StatusRegister == statusRegister.Value);
            }            var assignments = await query.ToListAsync(cancellationToken);

            // Mapeo a objetos de dominio con detalles
            return assignments.Select(ta => new TeachingAssignmentWithDetails
            {
                TeachingAssignment = ta,
                Teacher = ta.Teacher,
                TypeTeachingAssignment = ta.TypeTeachingAssignment
            }).ToList();
        }

        public Task<List<TeachingAssignment>> GetAssignmentsByTeacherAndCargoAsync(
            int teacherId,
            int cargoId,
            bool includeRevoked = false,
            CancellationToken cancellationToken = default)
        {
            return GetAssignmentsByTeacherAsync(teacherId, cargoId, includeRevoked, cancellationToken);
        }

        public async Task<List<TeachingAssignment>> GetAssignmentsByTeacherAsync(
            int teacherId,
            int? cargoId = null,
            bool includeRevoked = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<TeachingAssignment>()
                .AsNoTracking()
                .Include(ta => ta.TypeTeachingAssignment)
                .Include(ta => ta.InscriptionModality)
                    .ThenInclude(im => im.Modality)
                .Include(ta => ta.InscriptionModality)
                    .ThenInclude(im => im.StateInscription)
                .Where(ta => ta.IdTeacher == teacherId
                             && ta.StatusRegister
                             && ta.IsConfirmed
                             && ta.InscriptionModality.StatusRegister);

            if (cargoId.HasValue)
            {
                query = query.Where(ta => ta.IdTypeTeachingAssignment == cargoId.Value);
            }

            if (!includeRevoked)
            {
                query = query.Where(ta => ta.RevocationDate == null);
            }

            return await query
                .OrderByDescending(ta => ta.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> TryConfirmAssignmentAtomicallyAsync(
            int assignmentId,
            string? operationRegister,
            DateTimeOffset updatedAt,
            int? userId,
            CancellationToken cancellationToken = default)
        {
            if (_context.Database.IsRelational())
            {
                var rows = operationRegister != null
                    ? await _context.Set<TeachingAssignment>()
                        .Where(ta => ta.Id == assignmentId && ta.StatusRegister && !ta.IsConfirmed)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(ta => ta.IsConfirmed, true)
                            .SetProperty(ta => ta.OperationRegister, operationRegister)
                            .SetProperty(ta => ta.UpdatedAt, updatedAt)
                            .SetProperty(ta => ta.IdUserUpdatedAt, userId),
                            cancellationToken)
                    : await _context.Set<TeachingAssignment>()
                        .Where(ta => ta.Id == assignmentId && ta.StatusRegister && !ta.IsConfirmed)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(ta => ta.IsConfirmed, true)
                            .SetProperty(ta => ta.UpdatedAt, updatedAt)
                            .SetProperty(ta => ta.IdUserUpdatedAt, userId),
                            cancellationToken);

                var tracked = _context.Set<TeachingAssignment>().Local.FirstOrDefault(ta => ta.Id == assignmentId);
                if (tracked != null)
                    await _context.Entry(tracked).ReloadAsync(cancellationToken);

                return rows == 1;
            }

            var semaphore = AssignmentLocks.GetOrAdd(assignmentId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var entity = await _context.Set<TeachingAssignment>()
                    .FirstOrDefaultAsync(ta => ta.Id == assignmentId, cancellationToken);
                if (entity == null)
                    return false;

                await _context.Entry(entity).ReloadAsync(cancellationToken);
                if (_context.Entry(entity).State == EntityState.Detached || !entity.StatusRegister || entity.IsConfirmed)
                    return false;

                entity.IsConfirmed = true;
                if (operationRegister != null)
                    entity.OperationRegister = operationRegister;
                entity.UpdatedAt = updatedAt;
                entity.IdUserUpdatedAt = userId;

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<IAsyncDisposable> AcquireTeacherCargoQuotaLockAsync(
            int teacherId,
            int typeId,
            CancellationToken cancellationToken = default)
        {
            if (_context.Database.IsRelational())
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0}, {1})",
                    new object[] { teacherId, typeId },
                    cancellationToken);
                return NoopAsyncDisposable.Instance;
            }

            var semaphore = QuotaLocks.GetOrAdd((teacherId, typeId), _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);
            return new SemaphoreAsyncDisposable(semaphore);
        }

        private sealed class NoopAsyncDisposable : IAsyncDisposable
        {
            public static readonly NoopAsyncDisposable Instance = new();
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed class SemaphoreAsyncDisposable : IAsyncDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private int _disposed;

            public SemaphoreAsyncDisposable(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    _semaphore.Release();

                return ValueTask.CompletedTask;
            }
        }
    }
}
