using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
                .Where(p => inscriptionModalityIds.Contains(p.Id));

            // Aplicar filtros adicionales si existen
            if (filters != null && filters.Count > 0)
            {
                foreach (var filter in filters)
                {
                    // Ejemplo de filtrado por título
                    if (filter.Key.Equals("title", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(filter.Value))
                    {
                        var filterValue = filter.Value.ToLower();
                        proposalsQuery = proposalsQuery.Where(p => p.Title.ToLower().Contains(filterValue));
                    }
                    // Filtro por estado de propuesta
                    else if (filter.Key.Equals("stateProposalId", StringComparison.OrdinalIgnoreCase) && int.TryParse(filter.Value, out int stateProposalId))
                    {
                        proposalsQuery = proposalsQuery.Where(p => p.IdStateProposal == stateProposalId);
                    }
                    // Filtro por línea de investigación
                    else if (filter.Key.Equals("researchLineId", StringComparison.OrdinalIgnoreCase) && int.TryParse(filter.Value, out int researchLineId))
                    {
                        proposalsQuery = proposalsQuery.Where(p => p.IdResearchLine == researchLineId);
                    }
                    // Filtro por sublínea de investigación
                    else if (filter.Key.Equals("researchSubLineId", StringComparison.OrdinalIgnoreCase) && int.TryParse(filter.Value, out int researchSubLineId))
                    {
                        proposalsQuery = proposalsQuery.Where(p => p.IdResearchSubLine == researchSubLineId);
                    }
                }
            }
            
            // Contar total de registros antes de aplicar paginación
            var totalCount = await proposalsQuery.CountAsync(cancellationToken);

            // Aplicar ordenamiento
            IQueryable<Proposal> orderedQuery;
            if (!string.IsNullOrEmpty(sortBy))
            {
                // Mapeamos campos comunes que podrían venir en el sortBy
                switch (sortBy.ToLower())
                {
                    case "title":
                        orderedQuery = isDescending 
                            ? proposalsQuery.OrderByDescending(p => p.Title)
                            : proposalsQuery.OrderBy(p => p.Title);
                        break;
                    case "stateproposalname":
                        orderedQuery = isDescending 
                            ? proposalsQuery.OrderByDescending(p => p.StateProposal.Name)
                            : proposalsQuery.OrderBy(p => p.StateProposal.Name);
                        break;
                    case "researchlinename":
                        orderedQuery = isDescending 
                            ? proposalsQuery.OrderByDescending(p => p.ResearchLine.Name)
                            : proposalsQuery.OrderBy(p => p.ResearchLine.Name);
                        break;
                    case "createdat":
                        orderedQuery = isDescending 
                            ? proposalsQuery.OrderByDescending(p => p.CreatedAt)
                            : proposalsQuery.OrderBy(p => p.CreatedAt);
                        break;
                    case "updatedat":
                        orderedQuery = isDescending 
                            ? proposalsQuery.OrderByDescending(p => p.UpdatedAt)
                            : proposalsQuery.OrderBy(p => p.UpdatedAt);
                        break;
                    default:
                        // Ordenación por defecto (ID)
                        orderedQuery = isDescending 
                            ? proposalsQuery.OrderByDescending(p => p.Id)
                            : proposalsQuery.OrderBy(p => p.Id);
                        break;
                }
            }
            else
            {
                // Ordenación por ID por defecto
                orderedQuery = isDescending
                    ? proposalsQuery.OrderByDescending(p => p.Id)
                    : proposalsQuery.OrderBy(p => p.Id);
            }

            // Aplicar paginación y obtener las entidades con include
            var proposals = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.StateProposal)
                .Include(p => p.ResearchLine)
                .Include(p => p.ResearchSubLine)
                .Include(p => p.InscriptionModality)
                    .ThenInclude(im => im.TeachingAssignments)
                        .ThenInclude(ta => ta.Teacher)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            // Cargamos los usuarios para cada propuesta
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

            // Retornar resultado paginado
            return new PaginatedResult<ProposalWithDetails>
            {
                Items = result,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}