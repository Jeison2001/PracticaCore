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
            var orderByField = sortBy ?? string.Empty;            // 1. Paginar ProjectFinals primero
            var projectFinalsQuery = _context.ProjectFinals
                .Include(p => p.StateStage)
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
            var proposals = await _context.Set<Proposal>()                .Where(p => proposalIds.Contains(p.Id))
                .Include(x => x.StateStage)
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
            // 1. Obtener los IDs de modalidades donde participa el usuario
            var inscriptionModalityIds = await _context.Set<UserInscriptionModality>()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            if (!inscriptionModalityIds.Any())
                return new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>();            // 2. Traer ProjectFinals y sus relaciones
            var projects = await _context.ProjectFinals
                .Include(p => p.StateStage)
                .Where(p => inscriptionModalityIds.Contains(p.Id)
                            && (status == null || p.StatusRegister == status.Value))
                .ToListAsync();

            if (!projects.Any())
                return new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>();

            // 3. Traer las propuestas asociadas
            var proposalIds = projects.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => proposalIds.Contains(p.Id))
                .Include(x => x.StateStage)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .ToListAsync();

            // 4. Traer los estudiantes asociados
            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .ToListAsync();

            // 5. Armar el resultado
            return projects.Select(f => (
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
                .Include(p => p.StateStage)
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
                .Include(x => x.StateStage)
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
