using Domain.Common;
using Domain.Common.Teachers;
using Domain.Entities;
using Domain.Interfaces.Repositories;
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

        public async Task<PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters, long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false)
        {
            var orderByField = sortBy ?? string.Empty;
            // 1. Paginar ProjectFinals primero
            var projectFinalsQuery = _context.ProjectFinals
                .AsNoTracking()
                .AsQueryable();

            // Filtros por docente/cargo (claves enriquecidas) -> UN único EXISTS sobre la
            // asignación activa: si se combinan (p. ej. nombre + cargo), deben coincidir en
            // el MISMO docente (un docente puede tener varios cargos: director/jurado/asesor).
            var tf = TeacherAssignmentLoader.ExtractTeacherFilters(filters);
            var pf = ProposalFilterLoader.ExtractProjectFilters(tf.Remaining);
            var remainingFilters = pf.Remaining;
            if (tf.HasAny)
            {
                var namePattern = tf.Name != null ? TeacherAssignmentLoader.LikePattern(tf.Name) : null;
                var emailPattern = tf.Email != null ? TeacherAssignmentLoader.LikePattern(tf.Email) : null;
                var teacherId = tf.Id;
                var cargoId = tf.CargoId;
                var cargoCode = tf.CargoCode;
                var cargoName = tf.CargoName;
                projectFinalsQuery = projectFinalsQuery.Where(x => _context.Set<TeachingAssignment>().Any(ta =>
                    ta.IdInscriptionModality == x.Id && ta.StatusRegister && ta.RevocationDate == null &&
                    (namePattern == null || (ta.Teacher != null &&
                        (EF.Functions.ILike(EF.Functions.Unaccent(ta.Teacher.FirstName + " " + ta.Teacher.LastName), namePattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(ta.Teacher.FirstName), namePattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(ta.Teacher.LastName), namePattern)))) &&
                    (emailPattern == null || (ta.Teacher != null && EF.Functions.ILike(ta.Teacher.Email, emailPattern))) &&
                    (teacherId == null || ta.IdTeacher == teacherId) &&
                    (cargoId == null || ta.IdTypeTeachingAssignment == cargoId) &&
                    (cargoCode == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Code == cargoCode)) &&
                    (cargoName == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Name == cargoName))));
            }

            // Filtros sobre Proposal (comparte Id con la entidad raíz) -> EXISTS por propuesta:
            // title@like y researchline@like no son propiedades de ProjectFinal y
            // FilterBuilder las ignoraría si llegaran a ToPaginatedResultAsync.
            if (!string.IsNullOrWhiteSpace(pf.Title))
            {
                var titlePattern = TeacherAssignmentLoader.LikePattern(pf.Title);
                projectFinalsQuery = projectFinalsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.Title), titlePattern)));
            }

            if (!string.IsNullOrWhiteSpace(pf.ResearchLineName))
            {
                var linePattern = TeacherAssignmentLoader.LikePattern(pf.ResearchLineName);
                projectFinalsQuery = projectFinalsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.ResearchLine.Name), linePattern)));
            }

            // Aquí podrías aplicar filtros adicionales si lo deseas
            // projectFinalsQuery = ...

            // Con cursor (cursorId + cursorCreatedAt) la paginación es keyset
            // (CreatedAt DESC, Id DESC) con coste constante; skipTotalCount omite el COUNT.
            var paginatedResult = await projectFinalsQuery
                .ToPaginatedResultAsync<ProjectFinal, int>(
                    remainingFilters,
                    orderByField,
                    isDescending,
                    pageNumber,
                    pageSize,
                    default,
                    cursorId,
                    cursorCreatedAt,
                    skipTotalCount,
                    include: q => q.Include(p => p.StateStage));

            var finals = paginatedResult.Items.ToList();
            if (!finals.Any())
            {
                return new PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
                {
                    Items = new List<(ProjectFinal, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize,
                    HasMoreRows = paginatedResult.HasMoreRows
                };
            }

            // 2. Traer las propuestas asociadas
            var proposalIds = finals.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => proposalIds.Contains(p.Id))
                .Include(x => x.StateStage)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .AsNoTracking()
                .ToListAsync();

            // 3. Traer los estudiantes asociados
            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync();

            // 4. Traer los docentes asignados activos de la página
            var teachersByInscription = await TeacherAssignmentLoader
                .LoadActiveByInscriptionIdsAsync(_context, proposalIds);
            List<AssignedTeacher> TeachersFor(int id) =>
                teachersByInscription.TryGetValue(id, out var t) ? t : new List<AssignedTeacher>();

            var proposalsById = proposals.ToDictionary(p => p.Id);
            var studentsByModality = students
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 5. Armar el resultado
            var items = finals.Select(f => (
                f,
                proposalsById.TryGetValue(f.Id, out var prop) ? prop : null!,
                studentsByModality.TryGetValue(f.Id, out var sList) ? sList : new List<UserInscriptionModality>(),
                TeachersFor(f.Id)
            )).ToList();

            return new PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                HasMoreRows = paginatedResult.HasMoreRows
            };
        }

        public async Task<List<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null)
        {
            // 1. Obtener los IDs de modalidades donde participa el usuario
            var inscriptionModalityIds = await _context.Set<UserInscriptionModality>()
                .AsNoTracking()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            if (!inscriptionModalityIds.Any())
                return new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>();

            // 2. Traer ProjectFinals y sus relaciones
            var projects = await _context.ProjectFinals
                .AsNoTracking()
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
                .AsNoTracking()
                .ToListAsync();

            // 4. Traer los estudiantes asociados
            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => proposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync();

            var proposalsById = proposals.ToDictionary(p => p.Id);
            var studentsByModality = students
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 5. Armar el resultado
            return projects.Select(f => (
                f,
                proposalsById.TryGetValue(f.Id, out var prop) ? prop : null!,
                studentsByModality.TryGetValue(f.Id, out var sList) ? sList : new List<UserInscriptionModality>()
            )).ToList();
        }

        public async Task<PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters, long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false)
        {
            var proposalIds = await _context.Set<TeachingAssignment>()
                .AsNoTracking()
                .Where(ta => ta.IdTeacher == teacherId)
                .Select(ta => ta.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            if (!proposalIds.Any())
            {
                return new PaginatedResult<(ProjectFinal, Proposal, List<UserInscriptionModality>)>
                {
                    Items = new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>(),
                    TotalRecords = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    HasMoreRows = false
                };
            }

            var orderByField = sortBy ?? string.Empty;
            var projectFinalsQuery = _context.ProjectFinals
                .AsNoTracking()
                .Where(p => proposalIds.Contains(p.Id))
                .AsQueryable();

            // Filtros sobre Proposal (comparte Id con la entidad raíz) -> EXISTS por propuesta:
            // title@like y researchline@like se extraen para que no lleguen a FilterBuilder,
            // que los ignoraría por no ser propiedades de ProjectFinal.
            var pf = ProposalFilterLoader.ExtractProjectFilters(filters);
            if (!string.IsNullOrWhiteSpace(pf.Title))
            {
                var titlePattern = TeacherAssignmentLoader.LikePattern(pf.Title);
                projectFinalsQuery = projectFinalsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.Title), titlePattern)));
            }

            if (!string.IsNullOrWhiteSpace(pf.ResearchLineName))
            {
                var linePattern = TeacherAssignmentLoader.LikePattern(pf.ResearchLineName);
                projectFinalsQuery = projectFinalsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.ResearchLine.Name), linePattern)));
            }

            // Con cursor (cursorId + cursorCreatedAt) la paginación es keyset
            // (CreatedAt DESC, Id DESC) con coste constante; skipTotalCount omite el COUNT.
            var paginatedResult = await projectFinalsQuery
                .ToPaginatedResultAsync<ProjectFinal, int>(
                    pf.Remaining,
                    orderByField,
                    isDescending,
                    pageNumber,
                    pageSize,
                    default,
                    cursorId,
                    cursorCreatedAt,
                    skipTotalCount,
                    include: q => q.Include(p => p.StateStage));

            var finals = paginatedResult.Items.ToList();
            if (!finals.Any())
            {
                return new PaginatedResult<(ProjectFinal, Proposal, List<UserInscriptionModality>)>
                {
                    Items = new List<(ProjectFinal, Proposal, List<UserInscriptionModality>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize,
                    HasMoreRows = paginatedResult.HasMoreRows
                };
            }

            var resultProposalIds = finals.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => resultProposalIds.Contains(p.Id))
                .Include(x => x.StateStage)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => resultProposalIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync();

            var proposalsById = proposals.ToDictionary(p => p.Id);
            var studentsByModality = students
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = finals.Select(f => (
                f,
                proposalsById.TryGetValue(f.Id, out var prop) ? prop : null!,
                studentsByModality.TryGetValue(f.Id, out var sList) ? sList : new List<UserInscriptionModality>()
            )).ToList();

            return new PaginatedResult<(ProjectFinal Project, Proposal Proposal, List<UserInscriptionModality> Students)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                HasMoreRows = paginatedResult.HasMoreRows
            };
        }
    }
}
