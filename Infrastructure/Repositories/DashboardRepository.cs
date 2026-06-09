using Domain.Common.Dashboard;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Consultas de agregación para el dashboard. Construye un IQueryable base de
    /// InscriptionModality con los filtros y lo reutiliza para cada métrica.
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Filtros condicionados disponibles por modalidad (claves estables consumidas por el frontend).
        /// La metadata real (fases, estados, catálogos) se calcula en GetFilterOptionsAsync.
        /// </summary>
        private static readonly Dictionary<string, string[]> AvailableFiltersByModality = new()
        {
            ["PUBLICACION_ARTICULO"] = new[] { "stage", "stateStage", "journalCategory", "issn", "acceptanceDate" },
            ["PROYECTO_GRADO"] = new[] { "stage", "stateStage", "researchLine" },
            ["PRACTICA_ACADEMICA"] = new[] { "stage", "stateStage", "hours", "isEmprendimiento" },
            ["COTERMINAL"] = new[] { "stateStage", "average" },
            ["SEMINARIO_ACT"] = new[] { "stateStage", "grade", "attendance" },
            ["GRADO_PROMEDIO"] = new[] { "stateStage", "average", "hasFailedSubjects" },
            ["SABER_PRO"] = new[] { "stateStage", "score", "resultQuintile" },
        };

        private IQueryable<InscriptionModality> BuildBaseQuery(DashboardFilter filter)
        {
            var query = _context.Set<InscriptionModality>()
                .Where(i => i.StatusRegister);

            if (filter.DateFrom.HasValue)
                query = query.Where(i => i.CreatedAt >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(i => i.CreatedAt <= filter.DateTo.Value);
            if (filter.ModalityCodes is { Count: > 0 })
                query = query.Where(i => i.Modality != null && filter.ModalityCodes.Contains(i.Modality.Code));
            if (filter.StateCodes is { Count: > 0 })
                query = query.Where(i => i.StateInscription != null && filter.StateCodes.Contains(i.StateInscription.Code));
            if (filter.AcademicPeriodIds is { Count: > 0 })
                query = query.Where(i => filter.AcademicPeriodIds.Contains(i.IdAcademicPeriod));

            // Programa y facultad se resuelven vía el estudiante (UserInscriptionModality → User).
            if (filter.AcademicProgramIds is { Count: > 0 })
            {
                var programIds = filter.AcademicProgramIds;
                query = query.Where(i => i.UserInscriptionModalities.Any(uim =>
                    uim.StatusRegister && uim.User != null && programIds.Contains(uim.User.IdAcademicProgram)));
            }
            if (filter.FacultyIds is { Count: > 0 })
            {
                var facultyIds = filter.FacultyIds;
                query = query.Where(i => i.UserInscriptionModalities.Any(uim =>
                    uim.StatusRegister && uim.User != null && facultyIds.Contains(uim.User.AcademicProgram.IdFaculty)));
            }

            // Conteo de estudiantes vinculados (UserInscriptionModality activos).
            if (filter.MinStudents.HasValue)
                query = query.Where(i => i.UserInscriptionModalities.Count(uim => uim.StatusRegister) >= filter.MinStudents.Value);
            if (filter.MaxStudents.HasValue)
                query = query.Where(i => i.UserInscriptionModalities.Count(uim => uim.StatusRegister) <= filter.MaxStudents.Value);

            // Conteo de docentes asignados (TeachingAssignment activos y no revocados).
            if (filter.MinTeachers.HasValue)
                query = query.Where(i => i.TeachingAssignments.Count(ta => ta.StatusRegister && ta.RevocationDate == null) >= filter.MinTeachers.Value);
            if (filter.MaxTeachers.HasValue)
                query = query.Where(i => i.TeachingAssignments.Count(ta => ta.StatusRegister && ta.RevocationDate == null) <= filter.MaxTeachers.Value);
            if (filter.HasTeachingAssignment.HasValue)
            {
                if (filter.HasTeachingAssignment.Value)
                    query = query.Where(i => i.TeachingAssignments.Any(ta => ta.StatusRegister && ta.RevocationDate == null));
                else
                    query = query.Where(i => !i.TeachingAssignments.Any(ta => ta.StatusRegister && ta.RevocationDate == null));
            }

            // Filtros condicionados: solo cuando hay EXACTAMENTE una modalidad seleccionada.
            if (filter.ModalityScope is { } scope && filter.ModalityCodes is { Count: 1 })
                query = ApplyModalityScope(query, filter.ModalityCodes[0], scope);

            return query;
        }

        /// <summary>
        /// Aplica los filtros condicionados según la modalidad única seleccionada. Los estados de
        /// fase se resuelven por Code desde la entidad de extensión (no por Id), por lo que un cambio
        /// de IDs en la BD no rompe el filtro.
        /// </summary>
        private IQueryable<InscriptionModality> ApplyModalityScope(
            IQueryable<InscriptionModality> query, string modalityCode, ModalityScopedFilter scope)
        {
            // Fase actual de la inscripción (común, leído de InscriptionModality → StageModality).
            if (scope.StageOrders is { Count: > 0 })
                query = query.Where(i => i.StageModality != null && scope.StageOrders.Contains(i.StageModality.StageOrder));

            var stateCodes = scope.StateStageCodes;
            var hasStateCodes = stateCodes is { Count: > 0 };

            switch (modalityCode)
            {
                case "PUBLICACION_ARTICULO":
                    if (hasStateCodes)
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.StateStage != null
                            && stateCodes!.Contains(i.ScientificArticle.StateStage.Code));
                    if (scope.JournalCategories is { Count: > 0 })
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.JournalCategory != null
                            && scope.JournalCategories.Contains(i.ScientificArticle.JournalCategory));
                    if (!string.IsNullOrWhiteSpace(scope.Issn))
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.ISSN != null
                            && i.ScientificArticle.ISSN.Contains(scope.Issn!));
                    if (scope.AcceptanceFrom.HasValue)
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.AcceptanceDate >= scope.AcceptanceFrom.Value);
                    if (scope.AcceptanceTo.HasValue)
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.AcceptanceDate <= scope.AcceptanceTo.Value);
                    break;

                case "PROYECTO_GRADO":
                    // PG abarca 3 entidades (Proposal/PreliminaryProject/ProjectFinal), relacionadas 1:1
                    // con InscriptionModality por Id compartido. El estado de fase puede estar en cualquiera.
                    if (hasStateCodes)
                    {
                        var prelimIds = _context.Set<PreliminaryProject>()
                            .Where(p => p.StatusRegister && p.StateStage != null && stateCodes!.Contains(p.StateStage.Code))
                            .Select(p => p.Id);
                        var finalIds = _context.Set<ProjectFinal>()
                            .Where(p => p.StatusRegister && p.StateStage != null && stateCodes!.Contains(p.StateStage.Code))
                            .Select(p => p.Id);
                        query = query.Where(i =>
                            (i.Proposal != null && i.Proposal.StateStage != null && stateCodes!.Contains(i.Proposal.StateStage.Code))
                            || prelimIds.Contains(i.Id)
                            || finalIds.Contains(i.Id));
                    }
                    if (scope.ResearchLineIds is { Count: > 0 })
                        query = query.Where(i => i.Proposal != null && scope.ResearchLineIds.Contains(i.Proposal.IdResearchLine));
                    break;

                case "PRACTICA_ACADEMICA":
                    if (hasStateCodes)
                        query = query.Where(i => i.AcademicPractice != null
                            && i.AcademicPractice.StateStage != null
                            && stateCodes!.Contains(i.AcademicPractice.StateStage.Code));
                    if (scope.IsEmprendimiento.HasValue)
                        query = query.Where(i => i.AcademicPractice != null
                            && i.AcademicPractice.IsEmprendimiento == scope.IsEmprendimiento.Value);
                    if (scope.MinHours.HasValue)
                        query = query.Where(i => i.AcademicPractice != null && i.AcademicPractice.PracticeHours >= scope.MinHours.Value);
                    if (scope.MaxHours.HasValue)
                        query = query.Where(i => i.AcademicPractice != null && i.AcademicPractice.PracticeHours <= scope.MaxHours.Value);
                    break;

                case "COTERMINAL":
                    if (hasStateCodes)
                        query = query.Where(i => i.CoTerminal != null
                            && i.CoTerminal.StateStage != null
                            && stateCodes!.Contains(i.CoTerminal.StateStage.Code));
                    if (scope.MinAverage.HasValue)
                        query = query.Where(i => i.CoTerminal != null && i.CoTerminal.FirstSemesterAverage >= scope.MinAverage.Value);
                    if (scope.MaxAverage.HasValue)
                        query = query.Where(i => i.CoTerminal != null && i.CoTerminal.FirstSemesterAverage <= scope.MaxAverage.Value);
                    break;

                case "SEMINARIO_ACT":
                    if (hasStateCodes)
                        query = query.Where(i => i.Seminar != null
                            && i.Seminar.StateStage != null
                            && stateCodes!.Contains(i.Seminar.StateStage.Code));
                    if (scope.MinGrade.HasValue)
                        query = query.Where(i => i.Seminar != null && i.Seminar.FinalGrade >= scope.MinGrade.Value);
                    if (scope.MaxGrade.HasValue)
                        query = query.Where(i => i.Seminar != null && i.Seminar.FinalGrade <= scope.MaxGrade.Value);
                    if (scope.MinAttendance.HasValue)
                        query = query.Where(i => i.Seminar != null && i.Seminar.AttendancePercentage >= scope.MinAttendance.Value);
                    if (scope.MaxAttendance.HasValue)
                        query = query.Where(i => i.Seminar != null && i.Seminar.AttendancePercentage <= scope.MaxAttendance.Value);
                    break;

                case "GRADO_PROMEDIO":
                    if (hasStateCodes)
                        query = query.Where(i => i.AcademicAverage != null
                            && i.AcademicAverage.StateStage != null
                            && stateCodes!.Contains(i.AcademicAverage.StateStage.Code));
                    if (scope.MinAverage.HasValue)
                        query = query.Where(i => i.AcademicAverage != null && i.AcademicAverage.CertifiedAverage >= scope.MinAverage.Value);
                    if (scope.MaxAverage.HasValue)
                        query = query.Where(i => i.AcademicAverage != null && i.AcademicAverage.CertifiedAverage <= scope.MaxAverage.Value);
                    if (scope.HasFailedSubjects.HasValue)
                        query = query.Where(i => i.AcademicAverage != null
                            && i.AcademicAverage.HasFailedSubjects == scope.HasFailedSubjects.Value);
                    break;

                case "SABER_PRO":
                    if (hasStateCodes)
                        query = query.Where(i => i.SaberPro != null
                            && i.SaberPro.StateStage != null
                            && stateCodes!.Contains(i.SaberPro.StateStage.Code));
                    if (scope.MinScore.HasValue)
                        query = query.Where(i => i.SaberPro != null && i.SaberPro.ResultScore >= scope.MinScore.Value);
                    if (scope.MaxScore.HasValue)
                        query = query.Where(i => i.SaberPro != null && i.SaberPro.ResultScore <= scope.MaxScore.Value);
                    if (scope.ResultQuintiles is { Count: > 0 })
                        query = query.Where(i => i.SaberPro != null
                            && i.SaberPro.ResultQuintile != null
                            && scope.ResultQuintiles.Contains(i.SaberPro.ResultQuintile));
                    break;
            }

            return query;
        }

        public async Task<DashboardSummary> GetSummaryAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            var baseQuery = BuildBaseQuery(filter);

            // Nota: se proyecta a tipos anónimos y se mapea a records en memoria. EF Core (Npgsql)
            // no traduce la construcción de records con constructor posicional dentro del GROUP BY.
            var byStateRaw = await baseQuery
                .Where(i => i.StateInscription != null)
                .GroupBy(i => new { i.StateInscription!.Code, i.StateInscription.Name })
                .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byState = byStateRaw.Select(x => new CategoryCount(x.Code, x.Name, x.Count)).ToList();

            var total = byState.Sum(s => s.Count);
            int CountState(string code) => byState.FirstOrDefault(s => s.Key == code)?.Count ?? 0;
            var approved = CountState("APROBADO");

            var byModalityRaw = await baseQuery
                .Where(i => i.Modality != null)
                .GroupBy(i => new { i.Modality!.Code, i.Modality.Name })
                .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byModality = byModalityRaw
                .Select(x => new CategoryCount(x.Code, x.Name, x.Count))
                .OrderByDescending(c => c.Count)
                .ToList();

            var byPeriodRaw = await baseQuery
                .Where(i => i.AcademicPeriod != null)
                .GroupBy(i => i.AcademicPeriod!.Code)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byPeriod = byPeriodRaw
                .Select(x => new CategoryCount(x.Code, x.Code, x.Count))
                .OrderBy(c => c.Key)
                .ToList();

            var modalityByStateRaw = await baseQuery
                .Where(i => i.Modality != null && i.StateInscription != null)
                .GroupBy(i => new { i.Modality!.Code, i.Modality.Name, StateCode = i.StateInscription!.Code })
                .Select(g => new { g.Key.Code, g.Key.Name, g.Key.StateCode, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var modalityByState = modalityByStateRaw
                .Select(x => new ModalityStateCount(x.Code, x.Name, x.StateCode, x.Count))
                .ToList();

            // Programa y facultad: una inscripción puede tener varios estudiantes; se cuenta por estudiante vinculado.
            var byProgramRaw = await baseQuery
                .SelectMany(i => i.UserInscriptionModalities.Where(uim => uim.StatusRegister && uim.User != null))
                .GroupBy(uim => uim.User!.AcademicProgram.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byProgram = byProgramRaw
                .Select(x => new CategoryCount(x.Name, x.Name, x.Count))
                .OrderByDescending(c => c.Count)
                .ToList();

            var byFacultyRaw = await baseQuery
                .SelectMany(i => i.UserInscriptionModalities.Where(uim => uim.StatusRegister && uim.User != null))
                .GroupBy(uim => uim.User!.AcademicProgram.Faculty.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byFaculty = byFacultyRaw
                .Select(x => new CategoryCount(x.Name, x.Name, x.Count))
                .OrderByDescending(c => c.Count)
                .ToList();

            // Línea de tiempo por mes (yyyy-MM) según CreatedAt.
            var rawTimeline = await baseQuery
                .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var timeline = rawTimeline
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .Select(t => new TimePoint($"{t.Year:D4}-{t.Month:D2}", t.Count))
                .ToList();

            // Distribución por número de estudiantes vinculados (individual vs grupal).
            var studentCountRaw = await baseQuery
                .Select(i => i.UserInscriptionModalities.Count(uim => uim.StatusRegister))
                .GroupBy(c => c)
                .Select(g => new { Students = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byStudentCount = studentCountRaw
                .OrderBy(x => x.Students)
                .Select(x => new CategoryCount(
                    x.Students.ToString(),
                    x.Students == 1 ? "Individual (1)" : $"Grupal ({x.Students})",
                    x.Count))
                .ToList();

            // Asignaciones docentes activas de los registros filtrados (aplanadas).
            var activeAssignments = baseQuery
                .SelectMany(i => i.TeachingAssignments.Where(ta => ta.StatusRegister && ta.RevocationDate == null));

            // Carga docente: top 10 docentes por número de asignaciones activas.
            var teacherLoadRaw = await activeAssignments
                .Where(ta => ta.Teacher != null)
                .GroupBy(ta => new { ta.IdTeacher, ta.Teacher!.FirstName, ta.Teacher.LastName })
                .Select(g => new { g.Key.IdTeacher, g.Key.FirstName, g.Key.LastName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);
            var byTeacherLoad = teacherLoadRaw
                .Select(x => new CategoryCount(
                    x.IdTeacher.ToString(),
                    (x.FirstName + " " + x.LastName).Trim(),
                    x.Count))
                .ToList();

            // Asignaciones por tipo (Director / Co-director / Asesor / Jurado).
            var assignmentTypeRaw = await activeAssignments
                .Where(ta => ta.TypeTeachingAssignment != null)
                .GroupBy(ta => new { ta.TypeTeachingAssignment!.Code, ta.TypeTeachingAssignment.Name })
                .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byAssignmentType = assignmentTypeRaw
                .OrderByDescending(x => x.Count)
                .Select(x => new CategoryCount(x.Code, x.Name, x.Count))
                .ToList();

            return new DashboardSummary
            {
                TotalInscriptions = total,
                Approved = approved,
                Pending = CountState("PENDIENTE"),
                Rejected = CountState("RECHAZADO"),
                NotApplicable = CountState("NO_APLICA"),
                ApprovalRate = total > 0 ? Math.Round((double)approved / total * 100, 1) : 0,
                ByModality = byModality,
                ByState = byState,
                ByAcademicPeriod = byPeriod,
                ByProgram = byProgram,
                ByFaculty = byFaculty,
                Timeline = timeline,
                ModalityByState = modalityByState,
                ByStudentCount = byStudentCount,
                ByTeacherLoad = byTeacherLoad,
                ByAssignmentType = byAssignmentType
            };
        }

        public async Task<DashboardFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
        {
            var modalities = await _context.Set<Modality>()
                .Where(m => m.StatusRegister)
                .OrderBy(m => m.Name)
                .Select(m => new FilterOption(m.Code, m.Name))
                .ToListAsync(cancellationToken);

            var states = await _context.Set<StateInscription>()
                .OrderBy(s => s.Id)
                .Select(s => new FilterOption(s.Code, s.Name))
                .ToListAsync(cancellationToken);

            var periods = await _context.Set<AcademicPeriod>()
                .Where(p => p.StatusRegister)
                .OrderByDescending(p => p.Code)
                .Select(p => new FilterOption(p.Id.ToString(), p.Code))
                .ToListAsync(cancellationToken);

            var programs = await _context.Set<AcademicProgram>()
                .Where(p => p.StatusRegister)
                .OrderBy(p => p.Name)
                .Select(p => new FilterOption(p.Id.ToString(), p.Name))
                .ToListAsync(cancellationToken);

            var faculties = await _context.Set<Faculty>()
                .Where(f => f.StatusRegister)
                .OrderBy(f => f.Name)
                .Select(f => new FilterOption(f.Id.ToString(), f.Name))
                .ToListAsync(cancellationToken);

            var modalityFilters = await BuildModalityFiltersAsync(cancellationToken);

            return new DashboardFilterOptions
            {
                Modalities = modalities,
                States = states,
                AcademicPeriods = periods,
                Programs = programs,
                Faculties = faculties,
                ModalityFilters = modalityFilters
            };
        }

        /// <summary>
        /// Construye la metadata de filtros condicionados por modalidad: fases, estados de fase y
        /// catálogos discretos (categorías de revista, quintiles, líneas de investigación).
        /// </summary>
        private async Task<List<ModalityFilterMeta>> BuildModalityFiltersAsync(CancellationToken cancellationToken)
        {
            var modalities = await _context.Set<Modality>()
                .Where(m => m.StatusRegister)
                .Select(m => new { m.Id, m.Code, m.Name })
                .ToListAsync(cancellationToken);

            // Fases por modalidad.
            var stages = await _context.Set<StageModality>()
                .Where(s => s.StatusRegister)
                .Select(s => new { s.IdModality, s.StageOrder, s.Code, s.Name })
                .ToListAsync(cancellationToken);

            // Estados de fase con su fase (para saber a qué StageOrder pertenecen).
            var stateStages = await _context.Set<StateStage>()
                .Where(ss => ss.StatusRegister && ss.StageModality != null && ss.StageModality.StatusRegister)
                .Select(ss => new
                {
                    ss.Code,
                    ss.Name,
                    ss.StageModality!.IdModality,
                    ss.StageModality.StageOrder,
                    StageCode = ss.StageModality.Code
                })
                .ToListAsync(cancellationToken);

            // Catálogos discretos.
            var journalCategories = await _context.Set<ScientificArticle>()
                .Where(a => a.StatusRegister && a.JournalCategory != null && a.JournalCategory != "")
                .Select(a => a.JournalCategory!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            var resultQuintiles = await _context.Set<SaberPro>()
                .Where(s => s.StatusRegister && s.ResultQuintile != null && s.ResultQuintile != "")
                .Select(s => s.ResultQuintile!)
                .Distinct()
                .OrderBy(q => q)
                .ToListAsync(cancellationToken);

            var researchLines = await _context.Set<ResearchLine>()
                .Where(r => r.StatusRegister)
                .OrderBy(r => r.Name)
                .Select(r => new FilterOption(r.Id.ToString(), r.Name))
                .ToListAsync(cancellationToken);

            var result = new List<ModalityFilterMeta>();
            foreach (var m in modalities)
            {
                if (!AvailableFiltersByModality.TryGetValue(m.Code, out var available))
                    available = Array.Empty<string>();

                var meta = new ModalityFilterMeta
                {
                    ModalityCode = m.Code,
                    ModalityName = m.Name,
                    AvailableFilters = available.ToList(),
                    Stages = stages
                        .Where(s => s.IdModality == m.Id)
                        .OrderBy(s => s.StageOrder)
                        .Select(s => new StageOption(s.StageOrder, s.Code, s.Name))
                        .ToList(),
                    StateStages = stateStages
                        .Where(ss => ss.IdModality == m.Id)
                        .OrderBy(ss => ss.StageOrder)
                        .Select(ss => new StateStageOption(ss.Code, ss.Name, ss.StageOrder, ss.StageCode))
                        .ToList()
                };

                if (m.Code == "PUBLICACION_ARTICULO")
                    meta.JournalCategories = journalCategories;
                if (m.Code == "SABER_PRO")
                    meta.ResultQuintiles = resultQuintiles;
                if (m.Code == "PROYECTO_GRADO")
                    meta.ResearchLines = researchLines;

                result.Add(meta);
            }

            return result;
        }

        public async Task<ModalityBreakdown?> GetModalityBreakdownAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            // El drill-down solo aplica con exactamente una modalidad seleccionada.
            if (filter.ModalityCodes is not { Count: 1 })
                return null;

            var modalityCode = filter.ModalityCodes[0];
            var baseQuery = BuildBaseQuery(filter);

            var modality = await _context.Set<Modality>()
                .Where(m => m.Code == modalityCode)
                .Select(m => new { m.Code, m.Name })
                .FirstOrDefaultAsync(cancellationToken);
            if (modality == null)
                return null;

            var breakdown = new ModalityBreakdown
            {
                ModalityCode = modality.Code,
                ModalityName = modality.Name,
                Total = await baseQuery.CountAsync(cancellationToken)
            };

            // Embudo por fase actual (InscriptionModality → StageModality). NULL = aún sin aprobar.
            var phaseRaw = await baseQuery
                .Where(i => i.StageModality != null)
                .GroupBy(i => new { i.StageModality!.StageOrder, i.StageModality.Code, i.StageModality.Name })
                .Select(g => new { g.Key.StageOrder, g.Key.Code, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);
            breakdown.PhaseFunnel = phaseRaw
                .OrderBy(p => p.StageOrder)
                .Select(p => new PhasePoint(p.StageOrder, p.Code, p.Name, p.Count))
                .ToList();

            // Distribución por estado de fase (leído de la entidad de extensión, por Code).
            breakdown.ByStateStage = await GetStateStageDistributionAsync(modalityCode, baseQuery, cancellationToken);

            // Dimensiones específicas por modalidad.
            await FillModalitySpecificAsync(modalityCode, baseQuery, breakdown, cancellationToken);

            // Asignaciones por tipo: solo en modalidades con flujo de dirección/evaluación.
            if (modalityCode is "PROYECTO_GRADO" or "PRACTICA_ACADEMICA")
            {
                var assignTypeRaw = await baseQuery
                    .SelectMany(i => i.TeachingAssignments.Where(ta => ta.StatusRegister && ta.RevocationDate == null))
                    .Where(ta => ta.TypeTeachingAssignment != null)
                    .GroupBy(ta => new { ta.TypeTeachingAssignment!.Code, ta.TypeTeachingAssignment.Name })
                    .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                    .ToListAsync(cancellationToken);
                breakdown.AssignmentsByType = assignTypeRaw
                    .OrderByDescending(x => x.Count)
                    .Select(x => new CategoryCount(x.Code, x.Name, x.Count))
                    .ToList();
            }

            return breakdown;
        }

        /// <summary>Distribución por estado de fase (StateStage.Code/Name) de la entidad de extensión.</summary>
        private async Task<List<CategoryCount>> GetStateStageDistributionAsync(
            string modalityCode, IQueryable<InscriptionModality> baseQuery, CancellationToken ct)
        {
            // Se proyecta a anónimo y se mapea en memoria (EF/Npgsql no traduce records en GROUP BY).
            IQueryable<KeyName>? q = modalityCode switch
            {
                "PUBLICACION_ARTICULO" => baseQuery
                    .Where(i => i.ScientificArticle != null && i.ScientificArticle.StateStage != null)
                    .GroupBy(i => new { i.ScientificArticle!.StateStage!.Code, i.ScientificArticle.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                "PRACTICA_ACADEMICA" => baseQuery
                    .Where(i => i.AcademicPractice != null && i.AcademicPractice.StateStage != null)
                    .GroupBy(i => new { i.AcademicPractice!.StateStage!.Code, i.AcademicPractice.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                "COTERMINAL" => baseQuery
                    .Where(i => i.CoTerminal != null && i.CoTerminal.StateStage != null)
                    .GroupBy(i => new { i.CoTerminal!.StateStage!.Code, i.CoTerminal.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                "SEMINARIO_ACT" => baseQuery
                    .Where(i => i.Seminar != null && i.Seminar.StateStage != null)
                    .GroupBy(i => new { i.Seminar!.StateStage!.Code, i.Seminar.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                "GRADO_PROMEDIO" => baseQuery
                    .Where(i => i.AcademicAverage != null && i.AcademicAverage.StateStage != null)
                    .GroupBy(i => new { i.AcademicAverage!.StateStage!.Code, i.AcademicAverage.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                "SABER_PRO" => baseQuery
                    .Where(i => i.SaberPro != null && i.SaberPro.StateStage != null)
                    .GroupBy(i => new { i.SaberPro!.StateStage!.Code, i.SaberPro.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                // PG: el estado de fase vive en Proposal (entidad presente para todas las inscripciones PG).
                "PROYECTO_GRADO" => baseQuery
                    .Where(i => i.Proposal != null && i.Proposal.StateStage != null)
                    .GroupBy(i => new { i.Proposal!.StateStage.Code, i.Proposal.StateStage.Name })
                    .Select(g => new KeyName { Code = g.Key.Code, Name = g.Key.Name, Count = g.Count() }),
                _ => null
            };

            if (q == null) return new List<CategoryCount>();
            var raw = await q.ToListAsync(ct);
            return raw.Select(x => new CategoryCount(x.Code, x.Name, x.Count)).ToList();
        }

        /// <summary>Tipo de proyección intermedia para agregaciones (evita records en el GROUP BY).</summary>
        private sealed class KeyName
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public int Count { get; set; }
        }

        /// <summary>Rellena la distribución/histograma específico de cada modalidad.</summary>
        private async Task FillModalitySpecificAsync(
            string modalityCode, IQueryable<InscriptionModality> baseQuery, ModalityBreakdown b, CancellationToken ct)
        {
            switch (modalityCode)
            {
                case "PUBLICACION_ARTICULO":
                    b.DistributionLabel = "Categoría de revista";
                    var artRaw = await baseQuery.Where(i => i.ScientificArticle != null)
                        .GroupBy(i => i.ScientificArticle!.JournalCategory)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = artRaw
                        .Select(x => new CategoryCount(x.Key ?? "", x.Key ?? "Sin clasificar", x.Count))
                        .ToList();
                    break;

                case "SABER_PRO":
                    b.DistributionLabel = "Quintil de resultado";
                    var spRaw = await baseQuery.Where(i => i.SaberPro != null)
                        .GroupBy(i => i.SaberPro!.ResultQuintile)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = spRaw
                        .Select(x => new CategoryCount(x.Key ?? "", x.Key ?? "Sin dato", x.Count))
                        .ToList();
                    break;

                case "GRADO_PROMEDIO":
                    b.DistributionLabel = "¿Asignaturas perdidas?";
                    var gpRaw = await baseQuery.Where(i => i.AcademicAverage != null)
                        .GroupBy(i => i.AcademicAverage!.HasFailedSubjects)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = gpRaw
                        .Select(x => new CategoryCount(
                            x.Key == true ? "true" : (x.Key == false ? "false" : ""),
                            x.Key == true ? "Con pérdidas" : (x.Key == false ? "Sin pérdidas" : "Sin dato"),
                            x.Count))
                        .ToList();
                    break;

                case "PRACTICA_ACADEMICA":
                    b.DistributionLabel = "Tipo de práctica";
                    var paRaw = await baseQuery.Where(i => i.AcademicPractice != null)
                        .GroupBy(i => i.AcademicPractice!.IsEmprendimiento)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = paRaw
                        .Select(x => new CategoryCount(
                            x.Key ? "true" : "false",
                            x.Key ? "Emprendimiento" : "Práctica",
                            x.Count))
                        .ToList();
                    break;

                case "PROYECTO_GRADO":
                    b.DistributionLabel = "Línea de investigación";
                    var pgRaw = await baseQuery
                        .Where(i => i.Proposal != null && i.Proposal.ResearchLine != null)
                        .GroupBy(i => i.Proposal!.ResearchLine.Name)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = pgRaw
                        .Select(x => new CategoryCount(x.Name, x.Name, x.Count))
                        .OrderByDescending(c => c.Count)
                        .ToList();
                    break;
            }
        }

        public async Task<List<DashboardExportRow>> GetExportRowsAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            // Columnas específicas de modalidad solo cuando hay exactamente una seleccionada.
            var single = filter.ModalityCodes is { Count: 1 } ? filter.ModalityCodes[0] : null;

            var rows = await BuildBaseQuery(filter)
                .OrderBy(i => i.Id)
                .Select(i => new
                {
                    i.Id,
                    Modality = i.Modality != null ? i.Modality.Name : "",
                    State = i.StateInscription != null ? i.StateInscription.Name : "",
                    Period = i.AcademicPeriod != null ? i.AcademicPeriod.Code : "",
                    Students = i.UserInscriptionModalities
                        .Where(uim => uim.StatusRegister && uim.User != null)
                        .Select(uim => uim.User!.FirstName + " " + uim.User.LastName)
                        .ToList(),
                    Program = i.UserInscriptionModalities
                        .Where(uim => uim.StatusRegister && uim.User != null)
                        .Select(uim => uim.User!.AcademicProgram.Name)
                        .FirstOrDefault() ?? "",
                    Faculty = i.UserInscriptionModalities
                        .Where(uim => uim.StatusRegister && uim.User != null)
                        .Select(uim => uim.User!.AcademicProgram.Faculty.Name)
                        .FirstOrDefault() ?? "",
                    i.CreatedAt,
                    i.ApprovalDate,
                    CurrentPhase = i.StageModality != null ? i.StageModality.Name : "",
                    // Estado de fase y detalle de la entidad de extensión (según modalidad única).
                    PhaseState =
                        single == "PUBLICACION_ARTICULO" ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null ? i.ScientificArticle.StateStage.Name : "")
                        : single == "PRACTICA_ACADEMICA" ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null ? i.AcademicPractice.StateStage.Name : "")
                        : single == "COTERMINAL" ? (i.CoTerminal != null && i.CoTerminal.StateStage != null ? i.CoTerminal.StateStage.Name : "")
                        : single == "SEMINARIO_ACT" ? (i.Seminar != null && i.Seminar.StateStage != null ? i.Seminar.StateStage.Name : "")
                        : single == "GRADO_PROMEDIO" ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null ? i.AcademicAverage.StateStage.Name : "")
                        : single == "SABER_PRO" ? (i.SaberPro != null && i.SaberPro.StateStage != null ? i.SaberPro.StateStage.Name : "")
                        : single == "PROYECTO_GRADO" ? (i.Proposal != null && i.Proposal.StateStage != null ? i.Proposal.StateStage.Name : "")
                        : "",
                    Detail =
                        single == "PUBLICACION_ARTICULO" ? (i.ScientificArticle != null ? ((i.ScientificArticle.JournalCategory ?? "") + " | " + (i.ScientificArticle.JournalName ?? "")) : "")
                        : single == "PRACTICA_ACADEMICA" ? (i.AcademicPractice != null ? ((i.AcademicPractice.IsEmprendimiento ? "Emprendimiento" : "Práctica") + " | " + (i.AcademicPractice.InstitutionName ?? "")) : "")
                        : single == "COTERMINAL" ? (i.CoTerminal != null ? (i.CoTerminal.UniversityName ?? "") : "")
                        : single == "SEMINARIO_ACT" ? (i.Seminar != null ? (i.Seminar.SeminarName ?? "") : "")
                        : single == "GRADO_PROMEDIO" ? (i.AcademicAverage != null ? ("Promedio: " + (i.AcademicAverage.CertifiedAverage != null ? i.AcademicAverage.CertifiedAverage.ToString() : "-")) : "")
                        : single == "SABER_PRO" ? (i.SaberPro != null ? ("Quintil: " + (i.SaberPro.ResultQuintile ?? "-")) : "")
                        : single == "PROYECTO_GRADO" ? (i.Proposal != null ? i.Proposal.Title : "")
                        : ""
                })
                .ToListAsync(cancellationToken);

            return rows.Select(r => new DashboardExportRow(
                r.Id,
                r.Modality,
                r.State,
                r.Period,
                r.Program,
                r.Faculty,
                string.Join("; ", r.Students),
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                r.ApprovalDate?.ToString("yyyy-MM-dd") ?? "",
                r.CurrentPhase,
                r.PhaseState,
                r.Detail
            )).ToList();
        }
    }
}
