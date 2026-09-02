using Domain.Common.TeachingAssignments;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{    public class TeachingAssignmentRepository : BaseRepository<TeachingAssignment, int>, ITeachingAssignmentRepository
    {
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
    }
}
