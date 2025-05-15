using Domain.Entities;
using Domain.Interfaces;
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
    }
}
