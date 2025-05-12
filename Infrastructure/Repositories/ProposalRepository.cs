using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ProposalRepository : BaseRepository<Proposal, int>, IProposalRepository
    {
        private readonly AppDbContext _dbContext;

        public ProposalRepository(AppDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<List<ProposalWithDetails>> GetProposalsByTeacherWithDetailsAsync(
            int teacherId, 
            bool? status = null,
            CancellationToken cancellationToken = default)
        {
            // Consulta optimizada que inicia con las asignaciones de docentes
            var query = _dbContext.Set<TeachingAssignment>()
                .Where(ta => ta.IdTeacher == teacherId);
                
            if (status.HasValue)
            {
                query = query.Where(ta => ta.StatusRegister == status.Value);
            }

            // Obtener los IDs de las modalidades de inscripción
            var inscriptionModalityIds = await query
                .Select(ta => ta.IdInscriptionModality)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Si no hay inscripciones, retornar lista vacía
            if (!inscriptionModalityIds.Any())
            {
                return new List<ProposalWithDetails>();
            }
            
            // Consulta principal optimizada que incluye todas las relaciones necesarias
            var proposals = await _dbContext.Set<Proposal>()
                .Where(p => inscriptionModalityIds.Contains(p.Id))
                .Include(p => p.StateProposal)
                .Include(p => p.ResearchLine)
                .Include(p => p.ResearchSubLine)
                .Include(p => p.InscriptionModality)
                    .ThenInclude(im => im.TeachingAssignments)
                        .ThenInclude(ta => ta.Teacher)
                .AsSplitQuery() // Optimiza el rendimiento para consultas con muchas relaciones
                .ToListAsync(cancellationToken);

            // Cargamos los usuarios por separado para cada propuesta
            var result = new List<ProposalWithDetails>();
            
            foreach (var proposal in proposals)
            {
                var userInscriptionModalities = await _dbContext.Set<UserInscriptionModality>()
                    .Where(uim => uim.IdInscriptionModality == proposal.Id)
                    .Include(uim => uim.User)
                    .ToListAsync(cancellationToken);
                
                // Añadir a la lista de resultados
                result.Add(new ProposalWithDetails
                {
                    Proposal = proposal,
                    UserInscriptionModalities = userInscriptionModalities
                });
            }

            return result;
        }
    }
}