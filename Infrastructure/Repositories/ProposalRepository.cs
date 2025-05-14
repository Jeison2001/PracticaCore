using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Extensions; // for ToPaginatedResultAsync
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<PaginatedResult<ProposalWithDetails>> GetProposalsByTeacherWithDetailsPaginatedAsync(
            int teacherId,
            int pageNumber,
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string> filters,
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

            // Si no hay inscripciones, retornar resultado vacío
            if (!inscriptionModalityIds.Any())
            {
                return new PaginatedResult<ProposalWithDetails>
                {
                    Items = new List<ProposalWithDetails>(),
                    TotalRecords = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            
            // Consulta principal para obtener propuestas
            var proposalsQuery = _dbContext.Set<Proposal>()
                .Where(p => inscriptionModalityIds.Contains(p.Id))
                .Include(p => p.StateProposal)
                .Include(p => p.ResearchLine)
                .Include(p => p.ResearchSubLine)
                .Include(p => p.InscriptionModality)
                    .ThenInclude(im => im.TeachingAssignments)
                        .ThenInclude(ta => ta.Teacher)
                .AsNoTracking(); // Mejora el rendimiento para consultas de solo lectura

            // Agregar filtros específicos para campos que no son directamente propiedades de Proposal
            if (filters != null)
            {
                foreach (var filter in filters.ToList()) // Usar .ToList() para evitar modificación durante iteración
                {
                    // Manejar filtros especiales que no son parte de la entidad Proposal
                    if (filter.Key.Equals("title", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(filter.Value))
                    {
                        var filterValue = filter.Value.ToLower();
                        proposalsQuery = proposalsQuery.Where(p => p.Title.ToLower().Contains(filterValue));
                        filters.Remove(filter.Key); // Remover para que FilterBuilder no lo procese dos veces
                    }
                }
                
                // Los demás filtros se manejan automáticamente mediante FilterBuilder
            }

            // Preparar campo de ordenamiento dinámico
            var orderByField = sortBy ?? string.Empty;

            // Aplicar paginación, filtrado y ordenamiento con la extensión genérica
            var paginatedResult = await proposalsQuery
                .AsSplitQuery()
                .ToPaginatedResultAsync<Proposal, int>(
                    filters ?? new Dictionary<string, string>(),
                     orderByField,
                     isDescending,
                     pageNumber,
                     pageSize,
                     cancellationToken);

            var proposals = paginatedResult.Items.ToList();
            
            // Cargar todos los usuarios relacionados en una sola consulta
            var proposalIds = proposals.Select(p => p.Id).ToList();
            var userInscriptionModalities = await _dbContext.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Agrupar usuarios por modalidad de inscripción
            var usersByModality = userInscriptionModalities
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Construir el resultado final
            var result = proposals.Select(proposal => new ProposalWithDetails
            {
                Proposal = proposal,
                UserInscriptionModalities = usersByModality.ContainsKey(proposal.Id)
                    ? usersByModality[proposal.Id]
                    : new List<UserInscriptionModality>()
            }).ToList();

            // Retornar resultado paginado
            return new PaginatedResult<ProposalWithDetails>
            {
                Items = result,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<ProposalWithDetails>> GetProposalsByUserWithDetailsAsync(
            int userId,
            bool? status = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Obtener IDs de modalidades donde participa el usuario
            var inscriptionModalityIds = await _dbContext.Set<UserInscriptionModality>()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!inscriptionModalityIds.Any())
                return new List<ProposalWithDetails>();

            // 2. Traer propuestas y sus relaciones
            var proposals = await _dbContext.Set<Proposal>()
                .Where(p => inscriptionModalityIds.Contains(p.Id)
                            && (status == null || p.StatusRegister == status.Value))
                .Include(p => p.StateProposal)
                .Include(p => p.ResearchLine)
                .Include(p => p.ResearchSubLine)
                .Include(p => p.InscriptionModality)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (!proposals.Any())
                return new List<ProposalWithDetails>();

            // 3. Cargar todos los UserInscriptionModalities y usuarios en una sola consulta
            var proposalIds = proposals.Select(p => p.Id).ToList();
            var userInscriptionModalities = await _dbContext.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // 4. Agrupar por propuesta y construir resultado
            var usersByModality = userInscriptionModalities
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            return proposals.Select(p => new ProposalWithDetails
            {
                Proposal = p,
                UserInscriptionModalities = usersByModality.ContainsKey(p.Id)
                    ? usersByModality[p.Id]
                    : new List<UserInscriptionModality>()
            }).ToList();
        }
    }
}