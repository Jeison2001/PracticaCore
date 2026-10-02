using Domain.Common;
using Domain.Common.Teachers;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PreliminaryProjectRepository : BaseRepository<PreliminaryProject, int>, IPreliminaryProjectRepository
    {
        private new readonly AppDbContext _context;
        public PreliminaryProjectRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>> GetAllWithProposalAndStudentsAsync(int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            var orderByField = sortBy ?? string.Empty;            // 1. Paginar PreliminaryProjects primero
            var preliminaryProjectsQuery = _context.PreliminaryProjects
                .Include(p => p.StateStage)
                .AsQueryable();

            // Filtros por docente/cargo (claves enriquecidas) -> UN único EXISTS sobre la
            // asignación activa: si se combinan (p. ej. nombre + cargo), deben coincidir en
            // el MISMO docente (un docente puede tener varios cargos: director/jurado/asesor).
            var tf = TeacherAssignmentLoader.ExtractTeacherFilters(filters);
            var remainingFilters = tf.Remaining;
            if (tf.HasAny)
            {
                var namePattern = tf.Name != null ? TeacherAssignmentLoader.LikePattern(tf.Name) : null;
                var emailPattern = tf.Email != null ? TeacherAssignmentLoader.LikePattern(tf.Email) : null;
                var teacherId = tf.Id;
                var cargoId = tf.CargoId;
                var cargoCode = tf.CargoCode;
                var cargoName = tf.CargoName;
                preliminaryProjectsQuery = preliminaryProjectsQuery.Where(x => _context.Set<TeachingAssignment>().Any(ta =>
                    ta.IdInscriptionModality == x.Id && ta.StatusRegister && ta.RevocationDate == null &&
                    (namePattern == null || (ta.Teacher != null &&
                        (EF.Functions.ILike(ta.Teacher.FirstName + " " + ta.Teacher.LastName, namePattern) ||
                         EF.Functions.ILike(ta.Teacher.FirstName, namePattern) ||
                         EF.Functions.ILike(ta.Teacher.LastName, namePattern)))) &&
                    (emailPattern == null || (ta.Teacher != null && EF.Functions.ILike(ta.Teacher.Email, emailPattern))) &&
                    (teacherId == null || ta.IdTeacher == teacherId) &&
                    (cargoId == null || ta.IdTypeTeachingAssignment == cargoId) &&
                    (cargoCode == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Code == cargoCode)) &&
                    (cargoName == null || (ta.TypeTeachingAssignment != null && ta.TypeTeachingAssignment.Name == cargoName))));
            }

            // Aquí podrías aplicar filtros adicionales si lo deseas
            // preliminaryProjectsQuery = ...

            var paginatedResult = await preliminaryProjectsQuery
                .ToPaginatedResultAsync<PreliminaryProject, int>(
                    remainingFilters,
                    orderByField,
                    isDescending,
                    pageNumber,
                    pageSize);

            var projects = paginatedResult.Items.ToList();
            if (!projects.Any())
            {
                return new PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
                {
                    Items = new List<(PreliminaryProject, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize
                };
            }            // 2. Traer las propuestas asociadas
            var proposalIds = projects.Select(f => f.Id).ToList();
            var proposals = await _context.Set<Proposal>()
                .Where(p => proposalIds.Contains(p.Id))
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

            // 4. Traer los docentes asignados activos de la página
            var teachersByInscription = await TeacherAssignmentLoader
                .LoadActiveByInscriptionIdsAsync(_context, proposalIds);
            List<AssignedTeacher> TeachersFor(int id) =>
                teachersByInscription.TryGetValue(id, out var t) ? t : new List<AssignedTeacher>();

            // 5. Armar el resultado
            var items = projects.Select(f => (
                f,
                proposals.FirstOrDefault(p => p.Id == f.Id)!,
                students.Where(uim => uim.IdInscriptionModality == f.Id).ToList(),
                TeachersFor(f.Id)
            )).ToList();

            return new PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students, List<AssignedTeacher> Teachers)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };
        }

        public async Task<List<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByUserIdWithProposalAndStudentsAsync(int userId, bool? status = null)
        {
            // 1. Obtener los IDs de modalidades donde participa el usuario
            var inscriptionModalityIds = await _context.Set<UserInscriptionModality>()
                .Where(uim => uim.IdUser == userId)
                .Select(uim => uim.IdInscriptionModality)
                .Distinct()
                .ToListAsync();

            if (!inscriptionModalityIds.Any())
                return new List<(PreliminaryProject, Proposal, List<UserInscriptionModality>)>();            // 2. Traer PreliminaryProjects y sus relaciones
            var projects = await _context.PreliminaryProjects
                .Include(p => p.StateStage)
                .Where(p => inscriptionModalityIds.Contains(p.Id)
                            && (status == null || p.StatusRegister == status.Value))
                .ToListAsync();

            if (!projects.Any())
                return new List<(PreliminaryProject, Proposal, List<UserInscriptionModality>)>();            // 3. Traer las propuestas asociadas
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

        public async Task<PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>> GetByTeacherIdWithProposalAndStudentsAsync(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            var proposalIds = await _context.Set<TeachingAssignment>()
                .Where(ta => ta.IdTeacher == teacherId)
                .Select(ta => ta.IdInscriptionModality)
                .Distinct()
                .ToListAsync();            var orderByField = sortBy ?? string.Empty;
            var preliminaryProjectsQuery = _context.PreliminaryProjects
                .Include(p => p.StateStage)
                .Where(p => proposalIds.Contains(p.Id))
                .AsQueryable();

            var paginatedResult = await preliminaryProjectsQuery
                .ToPaginatedResultAsync<PreliminaryProject, int>(
                    filters ?? new Dictionary<string, string>(),
                    orderByField,
                    isDescending,
                    pageNumber,
                    pageSize);

            var projects = paginatedResult.Items.ToList();
            if (!projects.Any())
            {
                return new PaginatedResult<(PreliminaryProject, Proposal, List<UserInscriptionModality>)>
                {
                    Items = new List<(PreliminaryProject, Proposal, List<UserInscriptionModality>)>(),
                    TotalRecords = paginatedResult.TotalRecords,
                    PageNumber = paginatedResult.PageNumber,
                    PageSize = paginatedResult.PageSize
                };
            }            var resultProposalIds = projects.Select(f => f.Id).ToList();
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

            var items = projects.Select(f => (
                f,
                proposals.FirstOrDefault(p => p.Id == f.Id)!,
                students.Where(uim => uim.IdInscriptionModality == f.Id).ToList()
            )).ToList();

            return new PaginatedResult<(PreliminaryProject Project, Proposal Proposal, List<UserInscriptionModality> Students)>
            {
                Items = items,
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };
        }
    }
}
