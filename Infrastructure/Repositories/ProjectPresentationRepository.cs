using Domain.Common;
using Domain.Common.Teachers;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ProjectPresentationRepository : BaseRepository<ProjectPresentation, int>, IProjectPresentationRepository
    {
        private new readonly AppDbContext _context;

        public ProjectPresentationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        private static string LikePattern(string value) => $"%{value.Trim().Replace(" ", "%")}%";

        public async Task<PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetAllWithProposalAndStudentsAsync(
            int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters,
            long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false)
        {
            var orderByField = sortBy ?? string.Empty;
            var presentationsQuery = _context.ProjectPresentations
                .AsNoTracking()
                .AsQueryable();

            // 1. Filtros por docente/cargo
            var tf = TeacherAssignmentLoader.ExtractTeacherFilters(filters);
            var pf = ProposalFilterLoader.ExtractProjectFilters(tf.Remaining);
            var remainingFilters = new Dictionary<string, string>(pf.Remaining);

            if (tf.HasAny)
            {
                var namePattern = tf.Name != null ? TeacherAssignmentLoader.LikePattern(tf.Name) : null;
                var emailPattern = tf.Email != null ? TeacherAssignmentLoader.LikePattern(tf.Email) : null;
                var teacherId = tf.Id;
                var cargoId = tf.CargoId;
                var cargoCode = tf.CargoCode;
                var cargoName = tf.CargoName;
                presentationsQuery = presentationsQuery.Where(x => _context.Set<TeachingAssignment>().Any(ta =>
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

            // 2. Filtros sobre Proposal (title, researchline)
            if (!string.IsNullOrWhiteSpace(pf.Title))
            {
                var titlePattern = TeacherAssignmentLoader.LikePattern(pf.Title);
                presentationsQuery = presentationsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.Title), titlePattern)));
            }

            if (!string.IsNullOrWhiteSpace(pf.ResearchLineName))
            {
                var linePattern = TeacherAssignmentLoader.LikePattern(pf.ResearchLineName);
                presentationsQuery = presentationsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.ResearchLine.Name), linePattern)));
            }

            // 3. Filtro por StudentName si viene en filtros
            var studentFilterKeys = remainingFilters.Keys
                .Where(k => k.StartsWith("studentname", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in studentFilterKeys)
            {
                var studentNameVal = remainingFilters[key];
                remainingFilters.Remove(key);
                if (!string.IsNullOrWhiteSpace(studentNameVal))
                {
                    var pattern = LikePattern(studentNameVal);
                    presentationsQuery = presentationsQuery.Where(x => _context.Set<UserInscriptionModality>().Any(uim =>
                        uim.IdInscriptionModality == x.Id &&
                        uim.StatusRegister &&
                        uim.User != null &&
                        (EF.Functions.ILike(EF.Functions.Unaccent(uim.User.FirstName + " " + uim.User.LastName), pattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(uim.User.FirstName), pattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(uim.User.LastName), pattern) ||
                         EF.Functions.ILike(uim.User.Identification, pattern))));
                }
            }

            // 4. Paginar ProjectPresentations
            var paginatedResult = await presentationsQuery
                .ToPaginatedResultAsync<ProjectPresentation, int>(
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

            var presentations = paginatedResult.Items.ToList();
            if (!presentations.Any())
            {
                return new PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
                {
                    Items = new List<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize,
                    HasMoreRows = paginatedResult.HasMoreRows
                };
            }

            // 5. Cargar datos relacionados por IDs
            var presentationIds = presentations.Select(p => p.Id).ToList();

            var proposals = await _context.Set<Proposal>()
                .Where(p => presentationIds.Contains(p.Id))
                .Include(x => x.StateStage)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .AsNoTracking()
                .ToListAsync();

            var projectFinals = await _context.ProjectFinals
                .Where(pf => presentationIds.Contains(pf.Id))
                .Include(pf => pf.StateStage)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => presentationIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync();

            var teachersByInscription = await TeacherAssignmentLoader
                .LoadActiveByInscriptionIdsAsync(_context, presentationIds);

            List<AssignedTeacher> TeachersFor(int id) =>
                teachersByInscription.TryGetValue(id, out var t) ? t : new List<AssignedTeacher>();

            var proposalsById = proposals.ToDictionary(p => p.Id);
            var projectFinalsById = projectFinals.ToDictionary(pf => pf.Id);
            var studentsByModality = students
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = presentations.Select(p => (
                p,
                projectFinalsById.TryGetValue(p.Id, out var pf) ? pf : (ProjectFinal?)null,
                proposalsById.TryGetValue(p.Id, out var prop) ? prop : null!,
                studentsByModality.TryGetValue(p.Id, out var sList) ? sList : new List<UserInscriptionModality>(),
                TeachersFor(p.Id)
            )).ToList();

            return new PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                HasMoreRows = paginatedResult.HasMoreRows
            };
        }

        public async Task<List<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null)
        {
            var inscriptionModalityIds = await _context.Set<UserInscriptionModality>()
                .AsNoTracking()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            if (!inscriptionModalityIds.Any())
                return new List<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>();

            var presentations = await _context.ProjectPresentations
                .AsNoTracking()
                .Include(p => p.StateStage)
                .Where(p => inscriptionModalityIds.Contains(p.Id)
                            && (status == null || p.StatusRegister == status.Value))
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            if (!presentations.Any())
                return new List<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>();

            var presentationIds = presentations.Select(p => p.Id).ToList();

            var proposals = await _context.Set<Proposal>()
                .Where(p => presentationIds.Contains(p.Id))
                .Include(x => x.StateStage)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .AsNoTracking()
                .ToListAsync();

            var projectFinals = await _context.ProjectFinals
                .Where(pf => presentationIds.Contains(pf.Id))
                .Include(pf => pf.StateStage)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => presentationIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync();

            var teachersByInscription = await TeacherAssignmentLoader
                .LoadActiveByInscriptionIdsAsync(_context, presentationIds);

            var proposalsById = proposals.ToDictionary(p => p.Id);
            var projectFinalsById = projectFinals.ToDictionary(pf => pf.Id);
            var studentsByModality = students
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            return presentations.Select(p => (
                p,
                projectFinalsById.TryGetValue(p.Id, out var pf) ? pf : (ProjectFinal?)null,
                proposalsById.TryGetValue(p.Id, out var prop) ? prop : null!,
                studentsByModality.TryGetValue(p.Id, out var sList) ? sList : new List<UserInscriptionModality>(),
                teachersByInscription.TryGetValue(p.Id, out var tList) ? tList : new List<AssignedTeacher>()
            )).ToList();
        }

        public async Task<PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetByTeacherIdWithProposalAndStudentsAsync(
            int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters,
            long? cursorId = null, DateTimeOffset? cursorCreatedAt = null, bool skipTotalCount = false)
        {
            var proposalIds = await _context.Set<TeachingAssignment>()
                .AsNoTracking()
                .Where(ta => ta.IdTeacher == teacherId && ta.StatusRegister && ta.RevocationDate == null && ta.IsConfirmed)
                .Select(ta => ta.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            if (!proposalIds.Any())
            {
                return new PaginatedResult<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>
                {
                    Items = new List<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>(),
                    TotalRecords = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    HasMoreRows = false
                };
            }

            var orderByField = sortBy ?? string.Empty;
            var presentationsQuery = _context.ProjectPresentations
                .AsNoTracking()
                .Where(p => proposalIds.Contains(p.Id))
                .AsQueryable();

            var tf = TeacherAssignmentLoader.ExtractTeacherFilters(filters);
            var pf = ProposalFilterLoader.ExtractProjectFilters(tf.Remaining);
            presentationsQuery = presentationsQuery.ApplyTeacherFilterById(_context, tf);
            var remainingFilters = new Dictionary<string, string>(pf.Remaining);

            if (!string.IsNullOrWhiteSpace(pf.Title))
            {
                var titlePattern = TeacherAssignmentLoader.LikePattern(pf.Title);
                presentationsQuery = presentationsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.Title), titlePattern)));
            }

            if (!string.IsNullOrWhiteSpace(pf.ResearchLineName))
            {
                var linePattern = TeacherAssignmentLoader.LikePattern(pf.ResearchLineName);
                presentationsQuery = presentationsQuery.Where(x => _context.Set<Proposal>().Any(p =>
                    p.Id == x.Id && EF.Functions.ILike(EF.Functions.Unaccent(p.ResearchLine.Name), linePattern)));
            }

            var studentFilterKeys = remainingFilters.Keys
                .Where(k => k.StartsWith("studentname", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in studentFilterKeys)
            {
                var studentNameVal = remainingFilters[key];
                remainingFilters.Remove(key);
                if (!string.IsNullOrWhiteSpace(studentNameVal))
                {
                    var pattern = LikePattern(studentNameVal);
                    presentationsQuery = presentationsQuery.Where(x => _context.Set<UserInscriptionModality>().Any(uim =>
                        uim.IdInscriptionModality == x.Id &&
                        uim.StatusRegister &&
                        uim.User != null &&
                        (EF.Functions.ILike(EF.Functions.Unaccent(uim.User.FirstName + " " + uim.User.LastName), pattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(uim.User.FirstName), pattern) ||
                         EF.Functions.ILike(EF.Functions.Unaccent(uim.User.LastName), pattern) ||
                         EF.Functions.ILike(uim.User.Identification, pattern))));
                }
            }

            var paginatedResult = await presentationsQuery
                .ToPaginatedResultAsync<ProjectPresentation, int>(
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

            var presentations = paginatedResult.Items.ToList();
            if (!presentations.Any())
            {
                return new PaginatedResult<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>
                {
                    Items = new List<(ProjectPresentation, ProjectFinal?, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize,
                    HasMoreRows = paginatedResult.HasMoreRows
                };
            }

            var resultIds = presentations.Select(p => p.Id).ToList();

            var proposals = await _context.Set<Proposal>()
                .Where(p => resultIds.Contains(p.Id))
                .Include(x => x.StateStage)
                .Include(x => x.ResearchLine)
                .Include(x => x.ResearchSubLine)
                .Include(x => x.InscriptionModality)
                .AsNoTracking()
                .ToListAsync();

            var projectFinals = await _context.ProjectFinals
                .Where(pf2 => resultIds.Contains(pf2.Id))
                .Include(pf2 => pf2.StateStage)
                .AsNoTracking()
                .ToListAsync();

            var students = await _context.Set<UserInscriptionModality>()
                .Where(uim => resultIds.Contains(uim.IdInscriptionModality))
                .Include(uim => uim.User)
                .AsNoTracking()
                .ToListAsync();

            var teachersByInscription = await TeacherAssignmentLoader
                .LoadActiveByInscriptionIdsAsync(_context, resultIds);

            var proposalsById = proposals.ToDictionary(p => p.Id);
            var projectFinalsById = projectFinals.ToDictionary(pf2 => pf2.Id);
            var studentsByModality = students
                .GroupBy(uim => uim.IdInscriptionModality)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = presentations.Select(p => (
                p,
                projectFinalsById.TryGetValue(p.Id, out var pf2) ? pf2 : (ProjectFinal?)null,
                proposalsById.TryGetValue(p.Id, out var prop) ? prop : null!,
                studentsByModality.TryGetValue(p.Id, out var sList) ? sList : new List<UserInscriptionModality>(),
                teachersByInscription.TryGetValue(p.Id, out var tList) ? tList : new List<AssignedTeacher>()
            )).ToList();

            return new PaginatedResult<(ProjectPresentation Presentation, ProjectFinal? ProjectFinal, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
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
