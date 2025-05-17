using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ProjectFinalRepository : BaseRepository<ProjectFinal, int>, IProjectFinalRepository
    {
        private new readonly AppDbContext _context;
        public ProjectFinalRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            var orderByField = sortBy ?? string.Empty;
            // 1. Paginar ProjectFinals primero
            var projectFinalsQuery = _context.ProjectFinals
                .Include(p => p.StateProjectFinal)
                .AsQueryable();

            // Aquí podrías aplicar filtros adicionales si lo deseas
            // projectFinalsQuery = ...

            var paginatedResult = await projectFinalsQuery
                .ToPaginatedResultAsync<ProjectFinal, int>(
                    filters ?? new Dictionary<string, string>(),
                    orderByField,
                    isDescending,
                    pageNumber,
                    pageSize);

            var finals = paginatedResult.Items.ToList();
            if (!finals.Any())
            {
                return new PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>
                {
                    Items = new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize
                };
            }

            // 2. Traer las propuestas asociadas
            var proposalIds = finals.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => proposalIds.Contains(p.Id))
                .Include(x => x.StateProposal)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .ToListAsync();

            // 3. Traer los estudiantes asociados
            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .ToListAsync();

            // 4. Armar el resultado
            var items = finals.Select(f => (
                f,
                proposals.FirstOrDefault(p => p.Id == f.Id)!,
                students.Where(uim => uim.IdInscriptionModality == f.Id).ToList()
            )).ToList();

            return new PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };
        }

        public async Task<List<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null)
        {
            // 1. Obtener los ids de ProjectFinals creados por el usuario
            var projectFinalsQuery = _context.ProjectFinals
                .Include(p => p.StateProjectFinal)
                .Where(p => p.IdUserCreatedAt == userId)
                .AsQueryable();

            if (status.HasValue)
                projectFinalsQuery = projectFinalsQuery.Where(p => p.StatusRegister == status.Value);

            var finals = await projectFinalsQuery.ToListAsync();
            if (!finals.Any())
                return new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>();

            // 2. Traer las propuestas asociadas
            var proposalIds = finals.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => proposalIds.Contains(p.Id))
                .Include(x => x.StateProposal)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .ToListAsync();

            // 3. Traer los estudiantes asociados
            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .ToListAsync();

            // 4. Armar el resultado
            return finals.Select(f => (
                f,
                proposals.FirstOrDefault(p => p.Id == f.Id)!,
                students.Where(uim => uim.IdInscriptionModality == f.Id).ToList()
            )).ToList();
        }

        public async Task<PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            var proposalIds = await _context.Set<TeachingAssignment>()
                .Where(ta => ta.IdTeacher == teacherId)
                .Select(ta => ta.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            var orderByField = sortBy ?? string.Empty;
            var projectFinalsQuery = _context.ProjectFinals
                .Include(p => p.StateProjectFinal)
                .Where(p => proposalIds.Contains(p.Id))
                .AsQueryable();

            var paginatedResult = await projectFinalsQuery
                .ToPaginatedResultAsync<ProjectFinal, int>(
                    filters ?? new Dictionary<string, string>(),
                    orderByField,
                    isDescending,
                    pageNumber,
                    pageSize);

            var finals = paginatedResult.Items.ToList();
            if (!finals.Any())
            {
                return new PaginatedResult<(ProjectFinal, Proposal, List<UserInscriptionModality>)>
                {
                    Items = new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize
                };
            }

            var resultProposalIds = finals.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => resultProposalIds.Contains(p.Id))
                .Include(x => x.StateProposal)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .ToListAsync();

            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => resultProposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .ToListAsync();

            var items = finals.Select(f => (
                f,
                proposals.FirstOrDefault(p => p.Id == f.Id)!,
                students.Where(uim => uim.IdInscriptionModality == f.Id).ToList()
            )).ToList();

            return new PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };
        }
    }
}
