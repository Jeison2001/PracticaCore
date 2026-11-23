using System.Globalization;
using Domain.Common.Dashboard;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Consultas de agregación, alertas operativas, paginación y exportación para el dashboard.
    /// Todas las consultas son de solo lectura (.AsNoTracking()) y agregan del lado SQL.
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private const string LifecycleCompleted = "COMPLETED";
        private const string LifecycleInProgress = "IN_PROGRESS";
        private const string LifecycleInReview = "IN_REVIEW";
        private const string LifecycleWithObservations = "WITH_OBSERVATIONS";
        private const string LifecycleRejected = "REJECTED";
        private const int MaxExportRows = 50_000;
        private const int HydrationChunkSize = 2_000;
        private const int Acuerdo015CalendarSlaDays = 35; // ~25 días hábiles (Acuerdo 015 Arts. 15 y 24)

        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

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

        /// <summary>Estados de etapa que representan revisión/evaluación/programación pendiente.</summary>
        private static readonly string[] InReviewStateStageCodes =
        {
            "PROP_RADICADA",
            "AP_RADICADO_PEND_ASIG_EVAL",
            "AP_EN_EVALUACION",
            "PFINF_RADICADO_EN_EVALUACION",
            "SUST_PENDIENTE_PROGRAMACION",
            "PA_INSCRIPCION_EN_REVISION",
            "PA_DESARROLLO_EN_REVISION",
            "PA_INFORME_FINAL_EN_REVISION",
            "CT_RADICADO",
            "SEM_RADICADO",
            "ART_INS_RADICADO",
            "ART_PUB_RADICADO",
            "GP_RADICADO",
            "SP_RADICADO"
        };

        /// <summary>Estados evaluados por jurados/evaluadores sujetos a SLA del Acuerdo 015 (~25 días hábiles).</summary>
        private static readonly string[] JuryEvaluationStateStageCodes =
        {
            "AP_EN_EVALUACION",
            "PFINF_RADICADO_EN_EVALUACION",
            "PA_INFORME_FINAL_EN_REVISION"
        };

        private static readonly (string Code, string Label)[] LifecycleOrder =
        {
            (LifecycleCompleted, "Completado"),
            (LifecycleInProgress, "En curso"),
            (LifecycleInReview, "En revisión"),
            (LifecycleWithObservations, "Con observaciones"),
            (LifecycleRejected, "Rechazado")
        };

        private static string GetLifecycleLabel(string code) => code switch
        {
            LifecycleCompleted => "Completado",
            LifecycleInProgress => "En curso",
            LifecycleInReview => "En revisión",
            LifecycleWithObservations => "Con observaciones",
            LifecycleRejected => "Rechazado",
            _ => code
        };

        private static string FormatStateInscriptionLabel(string code, string dbName) =>
            code == "NO_APLICA" ? "Automático (No requiere aval)" : dbName;

        private IQueryable<InscriptionModality> BuildBaseQuery(DashboardFilter filter)
        {
            var query = _context.Set<InscriptionModality>()
                .AsNoTracking()
                .Where(i => i.StatusRegister);

            // Rango de fechas en zona horaria Colombia (DateTo inclusivo hasta fin de día Colombia).
            if (filter.DateFrom.HasValue)
            {
                var fromUtc = DashboardTimeHelper.ToColombiaDayStartUtc(filter.DateFrom.Value);
                query = query.Where(i => i.CreatedAt >= fromUtc);
            }
            if (filter.DateTo.HasValue)
            {
                var toExclusiveUtc = DashboardTimeHelper.ToColombiaNextDayStartUtc(filter.DateTo.Value);
                query = query.Where(i => i.CreatedAt < toExclusiveUtc);
            }
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
                    uim.StatusRegister && uim.IsConfirmed && uim.User != null && programIds.Contains(uim.User.IdAcademicProgram)));
            }
            if (filter.FacultyIds is { Count: > 0 })
            {
                var facultyIds = filter.FacultyIds;
                query = query.Where(i => i.UserInscriptionModalities.Any(uim =>
                    uim.StatusRegister && uim.IsConfirmed && uim.User != null
                    && uim.User.AcademicProgram != null
                    && facultyIds.Contains(uim.User.AcademicProgram.IdFaculty)));
            }

            // Conteo de estudiantes vinculados (UserInscriptionModality activos y confirmados).
            if (filter.MinStudents.HasValue)
                query = query.Where(i => i.UserInscriptionModalities.Count(uim => uim.StatusRegister && uim.IsConfirmed) >= filter.MinStudents.Value);
            if (filter.MaxStudents.HasValue)
                query = query.Where(i => i.UserInscriptionModalities.Count(uim => uim.StatusRegister && uim.IsConfirmed) <= filter.MaxStudents.Value);

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

            // Filtro SQL por estado consolidado de ciclo de vida (proyección mínima para evitar anidamiento profundo).
            if (filter.LifecycleStatuses is { Count: > 0 })
            {
                var requested = filter.LifecycleStatuses
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                if (requested.Count > 0)
                {
                    var matchingIds = ProjectLifecycleFlags(query)
                        .Where(x => requested.Contains(
                            (x.StateCode == "RECHAZADO" || x.IsFinalRejection || x.EffectiveIsRejectionState)
                                ? LifecycleRejected
                            : (x.EffectiveIsFinalOverall && !x.EffectiveIsRejectionState)
                                ? LifecycleCompleted
                            : (x.EffectiveStateStageCode.Contains("OBSERVACIONES")
                               || x.EffectiveStateStageCode == "PROP_NO_PERTINENTE"
                               || x.EffectiveStateStageCode == "SUST_NO_APROBADA_REINTENTO")
                                ? LifecycleWithObservations
                            : (x.StateCode == "PENDIENTE" || InReviewStateStageCodes.Contains(x.EffectiveStateStageCode))
                                ? LifecycleInReview
                            : LifecycleInProgress))
                        .Select(x => x.Id);

                    query = query.Where(i => matchingIds.Contains(i.Id));
                }
            }

            return query;
        }

        /// <summary>
        /// Filtra únicamente inscripciones activas (excluye COMPLETED y REJECTED) en un predicado plano sin subconsultas.
        /// </summary>
        private static IQueryable<InscriptionModality> FilterActiveOnly(IQueryable<InscriptionModality> query)
        {
            return query.Where(i =>
                !i.IsFinalRejection
                && (i.StateInscription == null || i.StateInscription.Code != "RECHAZADO")
                && !(
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? (i.ProjectPresentation.StateStage.IsFinalStateForModalityOverall || i.ProjectPresentation.StateStage.IsRejectionState)
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? (i.ProjectFinal.StateStage.IsFinalStateForModalityOverall || i.ProjectFinal.StateStage.IsRejectionState)
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? (i.PreliminaryProject.StateStage.IsFinalStateForModalityOverall || i.PreliminaryProject.StateStage.IsRejectionState)
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? (i.Proposal.StateStage.IsFinalStateForModalityOverall || i.Proposal.StateStage.IsRejectionState)
                        : false)
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && (i.AcademicPractice.StateStage.IsFinalStateForModalityOverall || i.AcademicPractice.StateStage.IsRejectionState))
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && (i.CoTerminal.StateStage.IsFinalStateForModalityOverall || i.CoTerminal.StateStage.IsRejectionState))
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null && (i.Seminar.StateStage.IsFinalStateForModalityOverall || i.Seminar.StateStage.IsRejectionState))
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && (i.ScientificArticle.StateStage.IsFinalStateForModalityOverall || i.ScientificArticle.StateStage.IsRejectionState))
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && (i.AcademicAverage.StateStage.IsFinalStateForModalityOverall || i.AcademicAverage.StateStage.IsRejectionState))
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null && (i.SaberPro.StateStage.IsFinalStateForModalityOverall || i.SaberPro.StateStage.IsRejectionState))
                    : false));
        }

        private static IQueryable<InscriptionModality> ApplySearch(IQueryable<InscriptionModality> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var term = search.Trim().ToLower();
            var hasId = int.TryParse(term, out var idSearch);

            return query.Where(i =>
                (hasId && i.Id == idSearch)
                || (i.Modality != null && i.Modality.Name.ToLower().Contains(term))
                || (i.AcademicPeriod != null && i.AcademicPeriod.Code.ToLower().Contains(term))
                || i.UserInscriptionModalities.Any(uim =>
                    uim.StatusRegister && uim.IsConfirmed && uim.User != null && (
                        (uim.User.FirstName + " " + uim.User.LastName).ToLower().Contains(term)
                        || uim.User.Identification.ToLower().Contains(term)
                        || uim.User.Email.ToLower().Contains(term)))
                || i.TeachingAssignments.Any(ta =>
                    ta.StatusRegister && ta.RevocationDate == null && ta.Teacher != null
                    && (ta.Teacher.FirstName + " " + ta.Teacher.LastName).ToLower().Contains(term))
                || (i.Proposal != null && i.Proposal.Title.ToLower().Contains(term))
                || (i.AcademicPractice != null && (
                    i.AcademicPractice.Title.ToLower().Contains(term)
                    || (i.AcademicPractice.InstitutionName != null && i.AcademicPractice.InstitutionName.ToLower().Contains(term))))
                || (i.ScientificArticle != null && (
                    (i.ScientificArticle.ArticleTitle != null && i.ScientificArticle.ArticleTitle.ToLower().Contains(term))
                    || (i.ScientificArticle.JournalName != null && i.ScientificArticle.JournalName.ToLower().Contains(term))
                    || (i.ScientificArticle.ISSN != null && i.ScientificArticle.ISSN.ToLower().Contains(term))))
                || (i.CoTerminal != null && (
                    (i.CoTerminal.UniversityName != null && i.CoTerminal.UniversityName.ToLower().Contains(term))
                    || (i.CoTerminal.PostgraduateProgramName != null && i.CoTerminal.PostgraduateProgramName.ToLower().Contains(term))))
                || (i.Seminar != null && i.Seminar.SeminarName != null && i.Seminar.SeminarName.ToLower().Contains(term)));
        }

        private IQueryable<InscriptionModality> ApplyModalityScope(
            IQueryable<InscriptionModality> query, string modalityCode, ModalityScopedFilter scope)
        {
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
                    {
                        var accFromUtc = DashboardTimeHelper.ToColombiaDayStartUtc(scope.AcceptanceFrom.Value);
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.AcceptanceDate >= accFromUtc);
                    }
                    if (scope.AcceptanceTo.HasValue)
                    {
                        var accToExclusiveUtc = DashboardTimeHelper.ToColombiaNextDayStartUtc(scope.AcceptanceTo.Value);
                        query = query.Where(i => i.ScientificArticle != null
                            && i.ScientificArticle.AcceptanceDate < accToExclusiveUtc);
                    }
                    break;

                case "PROYECTO_GRADO":
                    // Coincide exactamente con el estado de la fase ACTIVA (4 → 3 → 2 → 1).
                    if (hasStateCodes)
                    {
                        query = query.Where(i => stateCodes!.Contains(
                            i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                                ? i.ProjectPresentation.StateStage.Code
                            : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                                ? i.ProjectFinal.StateStage.Code
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.Code
                            : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                ? i.Proposal.StateStage.Code
                            : ""));
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

        /// <summary>Proyección liviana de banderas de ciclo de vida (sin nombres ni fechas de actividad).</summary>
        private static IQueryable<LifecycleFlagProjection> ProjectLifecycleFlags(IQueryable<InscriptionModality> query)
        {
            return query.Select(i => new LifecycleFlagProjection
            {
                Id = i.Id,
                StateCode = i.StateInscription != null ? i.StateInscription.Code : "",
                IsFinalRejection = i.IsFinalRejection,
                EffectiveStateStageCode =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.Code
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.Code
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.Code
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.Code
                        : "")
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null ? i.AcademicPractice.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null ? i.CoTerminal.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null ? i.Seminar.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null ? i.ScientificArticle.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null ? i.AcademicAverage.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null ? i.SaberPro.StateStage.Code : "")
                    : "",
                EffectiveIsFinalOverall =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.IsFinalStateForModalityOverall
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.IsFinalStateForModalityOverall
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.IsFinalStateForModalityOverall
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.IsFinalStateForModalityOverall
                        : false)
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && i.AcademicPractice.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && i.CoTerminal.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null && i.Seminar.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && i.ScientificArticle.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && i.AcademicAverage.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null && i.SaberPro.StateStage.IsFinalStateForModalityOverall)
                    : false,
                EffectiveIsRejectionState =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.IsRejectionState
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.IsRejectionState
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.IsRejectionState
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.IsRejectionState
                        : false)
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && i.AcademicPractice.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && i.CoTerminal.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null && i.Seminar.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && i.ScientificArticle.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && i.AcademicAverage.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null && i.SaberPro.StateStage.IsRejectionState)
                    : false
            });
        }

        /// <summary>
        /// Proyección SQL compacta (4 ramas PG) que resuelve el StateStage efectivo, ciclo de vida
        /// y marcas temporales de actividad y finalización.
        /// </summary>
        private static IQueryable<InscriptionProjection> ProjectInscriptions(IQueryable<InscriptionModality> query)
        {
            return query.Select(i => new InscriptionProjection
            {
                Id = i.Id,
                ModalityCode = i.Modality != null ? i.Modality.Code : "",
                ModalityName = i.Modality != null ? i.Modality.Name : "",
                MaximumTermPeriods = i.Modality != null ? i.Modality.MaximumTermPeriods : null,
                StateCode = i.StateInscription != null ? i.StateInscription.Code : "",
                StateName = i.StateInscription != null ? i.StateInscription.Name : "",
                IsFinalRejection = i.IsFinalRejection,
                IdAcademicPeriod = i.IdAcademicPeriod,
                PeriodCode = i.AcademicPeriod != null ? i.AcademicPeriod.Code : "",
                PeriodStartDate = i.AcademicPeriod != null ? (DateTimeOffset?)i.AcademicPeriod.StartDate : null,
                StageOrder = i.StageModality != null ? (int?)i.StageModality.StageOrder : null,
                StageCode = i.StageModality != null ? i.StageModality.Code : "",
                StageName = i.StageModality != null ? i.StageModality.Name : "",
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                ApprovalDate = i.ApprovalDate,

                EffectiveStateStageCode =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.Code
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.Code
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.Code
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.Code
                        : "")
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null ? i.AcademicPractice.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null ? i.CoTerminal.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null ? i.Seminar.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null ? i.ScientificArticle.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null ? i.AcademicAverage.StateStage.Code : "")
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null ? i.SaberPro.StateStage.Code : "")
                    : "",

                EffectiveStateStageName =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.Name
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.Name
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.Name
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.Name
                        : "")
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null ? i.AcademicPractice.StateStage.Name : "")
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null ? i.CoTerminal.StateStage.Name : "")
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null ? i.Seminar.StateStage.Name : "")
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null ? i.ScientificArticle.StateStage.Name : "")
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null ? i.AcademicAverage.StateStage.Name : "")
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null ? i.SaberPro.StateStage.Name : "")
                    : "",

                EffectiveIsFinalOverall =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.IsFinalStateForModalityOverall
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.IsFinalStateForModalityOverall
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.IsFinalStateForModalityOverall
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.IsFinalStateForModalityOverall
                        : false)
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && i.AcademicPractice.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && i.CoTerminal.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null && i.Seminar.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && i.ScientificArticle.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && i.AcademicAverage.StateStage.IsFinalStateForModalityOverall)
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null && i.SaberPro.StateStage.IsFinalStateForModalityOverall)
                    : false,

                EffectiveIsRejectionState =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? i.ProjectPresentation.StateStage.IsRejectionState
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.IsRejectionState
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? i.PreliminaryProject.StateStage.IsRejectionState
                        : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                            ? i.Proposal.StateStage.IsRejectionState
                        : false)
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA"
                        ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && i.AcademicPractice.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "COTERMINAL"
                        ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && i.CoTerminal.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT"
                        ? (i.Seminar != null && i.Seminar.StateStage != null && i.Seminar.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO"
                        ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && i.ScientificArticle.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO"
                        ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && i.AcademicAverage.StateStage.IsRejectionState)
                    : i.Modality != null && i.Modality.Code == "SABER_PRO"
                        ? (i.SaberPro != null && i.SaberPro.StateStage != null && i.SaberPro.StateStage.IsRejectionState)
                    : false,

                ExtensionActivityAt =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                            ? (i.ProjectPresentation.UpdatedAt ?? i.ProjectPresentation.ActualDateTime ?? i.ProjectPresentation.CreatedAt)
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? (i.ProjectFinal.UpdatedAt ?? i.ProjectFinal.FinalPhaseApprovalDate ?? i.ProjectFinal.ReportApprovalDate ?? i.ProjectFinal.CreatedAt)
                        : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                            ? (i.PreliminaryProject.UpdatedAt ?? i.PreliminaryProject.ApprovalDate ?? i.PreliminaryProject.CreatedAt)
                        : i.Proposal != null && i.Proposal.StatusRegister
                            ? (i.Proposal.UpdatedAt ?? i.Proposal.CreatedAt)
                        : (i.UpdatedAt ?? i.CreatedAt))
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA" && i.AcademicPractice != null
                        ? (i.AcademicPractice.UpdatedAt ?? i.AcademicPractice.FinalApprovalDate ?? i.AcademicPractice.FinalReportApprovalDate ?? i.AcademicPractice.DevelopmentCompletionDate ?? i.AcademicPractice.PlanApprovalDate ?? i.AcademicPractice.AvalApprovalDate ?? i.AcademicPractice.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "COTERMINAL" && i.CoTerminal != null
                        ? (i.CoTerminal.UpdatedAt ?? i.CoTerminal.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT" && i.Seminar != null
                        ? (i.Seminar.UpdatedAt ?? i.Seminar.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO" && i.ScientificArticle != null
                        ? (i.ScientificArticle.UpdatedAt ?? i.ScientificArticle.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO" && i.AcademicAverage != null
                        ? (i.AcademicAverage.UpdatedAt ?? i.AcademicAverage.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "SABER_PRO" && i.SaberPro != null
                        ? (i.SaberPro.UpdatedAt ?? i.SaberPro.CreatedAt)
                    : (i.UpdatedAt ?? i.CreatedAt),

                CompletionDate =
                    i.Modality != null && i.Modality.Code == "PROYECTO_GRADO"
                        ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister
                            ? (i.ProjectPresentation.ActualDateTime ?? i.ProjectPresentation.UpdatedAt ?? i.ProjectPresentation.CreatedAt)
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister
                            ? (i.ProjectFinal.FinalPhaseApprovalDate ?? i.ProjectFinal.UpdatedAt ?? i.ProjectFinal.CreatedAt)
                        : (i.UpdatedAt ?? i.CreatedAt))
                    : i.Modality != null && i.Modality.Code == "PRACTICA_ACADEMICA" && i.AcademicPractice != null
                        ? (i.AcademicPractice.FinalApprovalDate ?? i.AcademicPractice.UpdatedAt ?? i.AcademicPractice.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "COTERMINAL" && i.CoTerminal != null
                        ? (i.CoTerminal.UpdatedAt ?? i.CoTerminal.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "SEMINARIO_ACT" && i.Seminar != null
                        ? (i.Seminar.UpdatedAt ?? i.Seminar.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "PUBLICACION_ARTICULO" && i.ScientificArticle != null
                        ? (i.ScientificArticle.UpdatedAt ?? i.ScientificArticle.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "GRADO_PROMEDIO" && i.AcademicAverage != null
                        ? (i.AcademicAverage.UpdatedAt ?? i.AcademicAverage.CreatedAt)
                    : i.Modality != null && i.Modality.Code == "SABER_PRO" && i.SaberPro != null
                        ? (i.SaberPro.UpdatedAt ?? i.SaberPro.CreatedAt)
                    : (i.UpdatedAt ?? i.CreatedAt)
            });
        }

        private static IQueryable<InscriptionLifecycleProjection> WithLifecycle(IQueryable<InscriptionProjection> projected)
        {
            return projected.Select(p => new InscriptionLifecycleProjection
            {
                Id = p.Id,
                ModalityCode = p.ModalityCode,
                ModalityName = p.ModalityName,
                MaximumTermPeriods = p.MaximumTermPeriods,
                StateCode = p.StateCode,
                StateName = p.StateName,
                IdAcademicPeriod = p.IdAcademicPeriod,
                PeriodCode = p.PeriodCode,
                PeriodStartDate = p.PeriodStartDate,
                StageOrder = p.StageOrder,
                StageCode = p.StageCode,
                StageName = p.StageName,
                CreatedAt = p.CreatedAt,
                ApprovalDate = p.ApprovalDate,
                EffectiveStateStageCode = p.EffectiveStateStageCode,
                EffectiveStateStageName = p.EffectiveStateStageName,
                LastActivityAt = p.UpdatedAt.HasValue && p.UpdatedAt.Value > p.ExtensionActivityAt
                    ? p.UpdatedAt.Value
                    : p.ExtensionActivityAt,
                CompletionDate = p.CompletionDate,
                LifecycleCode =
                    (p.StateCode == "RECHAZADO" || p.IsFinalRejection || p.EffectiveIsRejectionState)
                        ? LifecycleRejected
                    : (p.EffectiveIsFinalOverall && !p.EffectiveIsRejectionState)
                        ? LifecycleCompleted
                    : (p.EffectiveStateStageCode.Contains("OBSERVACIONES")
                       || p.EffectiveStateStageCode == "PROP_NO_PERTINENTE"
                       || p.EffectiveStateStageCode == "SUST_NO_APROBADA_REINTENTO")
                        ? LifecycleWithObservations
                    : (p.StateCode == "PENDIENTE" || InReviewStateStageCodes.Contains(p.EffectiveStateStageCode))
                        ? LifecycleInReview
                    : LifecycleInProgress
            });
        }

        public async Task<DashboardSummary> GetSummaryAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var baseQuery = BuildBaseQuery(filter);
            var lifecycleQuery = WithLifecycle(ProjectInscriptions(baseQuery));

            // 1. Una sola agregación SQL (máx ~35 filas) para Lifecycle, ByModality y ModalityByLifecycle.
            var modalityLifecycleRaw = await lifecycleQuery
                .GroupBy(x => new { x.ModalityCode, x.ModalityName, x.LifecycleCode })
                .Select(g => new { g.Key.ModalityCode, g.Key.ModalityName, g.Key.LifecycleCode, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var lifecycleMap = modalityLifecycleRaw
                .GroupBy(x => x.LifecycleCode)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            int CountLifecycle(string code) => lifecycleMap.TryGetValue(code, out var c) ? c : 0;

            var completed = CountLifecycle(LifecycleCompleted);
            var inProgress = CountLifecycle(LifecycleInProgress);
            var inReview = CountLifecycle(LifecycleInReview);
            var withObservations = CountLifecycle(LifecycleWithObservations);
            var rejectedOverall = CountLifecycle(LifecycleRejected);
            var total = completed + inProgress + inReview + withObservations + rejectedOverall;

            var byLifecycleStatus = LifecycleOrder
                .Select(item => new CategoryCount(item.Code, item.Label, CountLifecycle(item.Code)))
                .ToList();

            var byModality = modalityLifecycleRaw
                .Where(x => x.ModalityCode != "")
                .GroupBy(x => new { x.ModalityCode, x.ModalityName })
                .Select(g => new CategoryCount(g.Key.ModalityCode, g.Key.ModalityName, g.Sum(x => x.Count)))
                .OrderByDescending(c => c.Count)
                .ToList();

            var modalityByLifecycle = modalityLifecycleRaw
                .Where(x => x.ModalityCode != "")
                .Select(x => new ModalityLifecycleCount(
                    x.ModalityCode,
                    x.ModalityName,
                    x.LifecycleCode,
                    GetLifecycleLabel(x.LifecycleCode),
                    x.Count))
                .ToList();

            var completionRate = total > 0 ? Math.Round((double)completed / total * 100.0, 1) : 0.0;
            double? successRateOverClosed = (completed + rejectedOverall) > 0
                ? Math.Round((double)completed / (completed + rejectedOverall) * 100.0, 1)
                : null;

            // 2. Duración promedio de ciclo (días) para inscripciones COMPLETED (agregado SQL).
            double? avgCycleDays = null;
            if (completed > 0)
            {
                var avgDaysRaw = await lifecycleQuery
                    .Where(x => x.LifecycleCode == LifecycleCompleted)
                    .Select(x => (x.CompletionDate - x.CreatedAt).TotalDays)
                    .Select(d => d < 0 ? 0.0 : d)
                    .AverageAsync(cancellationToken);
                avgCycleDays = Math.Round(avgDaysRaw, 1);
            }

            // 3. Conteo de estancadas y sustentaciones próximas.
            var staleThreshold = filter.StaleDaysThreshold > 0 ? filter.StaleDaysThreshold : 30;
            var staleCutoffUtc = nowUtc.AddDays(-staleThreshold);

            var staleCount = await lifecycleQuery
                .CountAsync(x => x.LifecycleCode != LifecycleCompleted
                    && x.LifecycleCode != LifecycleRejected
                    && x.LastActivityAt <= staleCutoffUtc, cancellationToken);

            var upcomingDefensesCount = await baseQuery
                .CountAsync(i => !i.IsFinalRejection
                    && (i.StateInscription == null || i.StateInscription.Code != "RECHAZADO")
                    && i.ProjectPresentation != null
                    && i.ProjectPresentation.StatusRegister
                    && i.ProjectPresentation.StateStage != null
                    && i.ProjectPresentation.StateStage.Code == "SUST_PROGRAMADA"
                    && i.ProjectPresentation.ScheduledDateTime != null
                    && i.ProjectPresentation.ScheduledDateTime >= nowUtc, cancellationToken);

            // 4. Una agregación SQL para ModalityByState y ByState (máx ~28 filas).
            var modalityByStateRaw = await baseQuery
                .Where(i => i.StateInscription != null)
                .GroupBy(i => new
                {
                    ModalityCode = i.Modality != null ? i.Modality.Code : "",
                    ModalityName = i.Modality != null ? i.Modality.Name : "",
                    StateCode = i.StateInscription!.Code,
                    StateName = i.StateInscription.Name
                })
                .Select(g => new { g.Key.ModalityCode, g.Key.ModalityName, g.Key.StateCode, g.Key.StateName, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var byState = modalityByStateRaw
                .GroupBy(x => new { x.StateCode, x.StateName })
                .Select(g => new CategoryCount(
                    g.Key.StateCode,
                    FormatStateInscriptionLabel(g.Key.StateCode, g.Key.StateName),
                    g.Sum(x => x.Count)))
                .ToList();

            var modalityByState = modalityByStateRaw
                .Where(x => x.ModalityCode != "")
                .Select(x => new ModalityStateCount(
                    x.ModalityCode,
                    x.ModalityName,
                    x.StateCode,
                    x.Count,
                    FormatStateInscriptionLabel(x.StateCode, x.StateName)))
                .ToList();

            int CountState(string code) => byState.FirstOrDefault(s => s.Key == code)?.Count ?? 0;
            var approved = CountState("APROBADO");
            var pending = CountState("PENDIENTE");
            var rejected = CountState("RECHAZADO");
            var notApplicable = CountState("NO_APLICA");
            var reqApproval = total - notApplicable;
            var approvalRate = reqApproval > 0 ? Math.Round((double)approved / reqApproval * 100.0, 1) : 0.0;

            // 5. Una agregación SQL para PeriodByModality y ByAcademicPeriod.
            var periodByModalityRaw = await baseQuery
                .Where(i => i.AcademicPeriod != null)
                .GroupBy(i => new
                {
                    PeriodCode = i.AcademicPeriod!.Code,
                    ModalityCode = i.Modality != null ? i.Modality.Code : "",
                    ModalityName = i.Modality != null ? i.Modality.Name : ""
                })
                .Select(g => new { g.Key.PeriodCode, g.Key.ModalityCode, g.Key.ModalityName, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var byPeriod = periodByModalityRaw
                .GroupBy(x => x.PeriodCode)
                .Select(g => new CategoryCount(g.Key, g.Key, g.Sum(x => x.Count)))
                .OrderBy(c => c.Key)
                .ToList();

            var periodByModality = periodByModalityRaw
                .Where(x => x.ModalityCode != "")
                .OrderBy(x => x.PeriodCode)
                .ThenBy(x => x.ModalityName)
                .Select(x => new PeriodModalityCount(x.PeriodCode, x.ModalityCode, x.ModalityName, x.Count))
                .ToList();

            // 6. Programa y facultad (por estudiante confirmado).
            var byProgramRaw = await baseQuery
                .SelectMany(i => i.UserInscriptionModalities.Where(uim =>
                    uim.StatusRegister && uim.IsConfirmed && uim.User != null && uim.User.AcademicProgram != null))
                .GroupBy(uim => uim.User!.AcademicProgram.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byProgram = byProgramRaw
                .Select(x => new CategoryCount(x.Name, x.Name, x.Count))
                .OrderByDescending(c => c.Count)
                .ToList();

            var byFacultyRaw = await baseQuery
                .SelectMany(i => i.UserInscriptionModalities.Where(uim =>
                    uim.StatusRegister && uim.IsConfirmed && uim.User != null
                    && uim.User.AcademicProgram != null && uim.User.AcademicProgram.Faculty != null))
                .GroupBy(uim => uim.User!.AcademicProgram.Faculty.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var byFaculty = byFacultyRaw
                .Select(x => new CategoryCount(x.Name, x.Name, x.Count))
                .OrderByDescending(c => c.Count)
                .ToList();

            // 7. Línea de tiempo mensual (yyyy-MM en hora Colombia UTC-5).
            var rawTimeline = await baseQuery
                .Select(i => i.CreatedAt.UtcDateTime.AddHours(-5))
                .GroupBy(d => new { d.Year, d.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var timeline = rawTimeline
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .Select(t => new TimePoint($"{t.Year:D4}-{t.Month:D2}", t.Count))
                .ToList();

            // 8. Distribución por número de estudiantes vinculados.
            var studentCountRaw = await baseQuery
                .Select(i => i.UserInscriptionModalities.Count(uim => uim.StatusRegister && uim.IsConfirmed))
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

            // 9. Carga docente (predicado plano sin subconsulta anidada cuando ActiveTeachersOnly = true).
            var teacherScopedInscriptions = filter.ActiveTeachersOnly
                ? FilterActiveOnly(baseQuery)
                : baseQuery;

            var activeAssignments = teacherScopedInscriptions
                .SelectMany(i => i.TeachingAssignments.Where(ta => ta.StatusRegister && ta.RevocationDate == null));

            var teacherLoadRaw = await activeAssignments
                .Where(ta => ta.Teacher != null)
                .GroupBy(ta => new { ta.IdTeacher, ta.Teacher!.FirstName, ta.Teacher.LastName })
                .Select(g => new { g.Key.IdTeacher, g.Key.FirstName, g.Key.LastName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.FirstName)
                .Take(10)
                .ToListAsync(cancellationToken);

            var topTeacherIds = teacherLoadRaw.Select(t => t.IdTeacher).ToList();
            var teacherRolesMap = new Dictionary<int, Dictionary<string, int>>();

            if (topTeacherIds.Count > 0)
            {
                var teacherRolesRaw = await activeAssignments
                    .Where(ta => topTeacherIds.Contains(ta.IdTeacher) && ta.TypeTeachingAssignment != null)
                    .GroupBy(ta => new { ta.IdTeacher, RoleName = ta.TypeTeachingAssignment!.Name })
                    .Select(g => new { g.Key.IdTeacher, g.Key.RoleName, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                foreach (var row in teacherRolesRaw)
                {
                    if (!teacherRolesMap.TryGetValue(row.IdTeacher, out var roleDict))
                    {
                        roleDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        teacherRolesMap[row.IdTeacher] = roleDict;
                    }
                    roleDict[row.RoleName] = row.Count;
                }
            }

            var byTeacherLoad = teacherLoadRaw
                .Select(x => new CategoryCount(
                    x.IdTeacher.ToString(),
                    (x.FirstName + " " + x.LastName).Trim(),
                    x.Count))
                .ToList();

            var teacherLoadDetailed = teacherLoadRaw
                .Select(x => new TeacherLoadDetail(
                    x.IdTeacher,
                    (x.FirstName + " " + x.LastName).Trim(),
                    x.Count,
                    teacherRolesMap.TryGetValue(x.IdTeacher, out var roles)
                        ? roles
                        : new Dictionary<string, int>()))
                .ToList();

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
                Pending = pending,
                Rejected = rejected,
                NotApplicable = notApplicable,
                ApprovalRate = approvalRate,
                Completed = completed,
                InReview = inReview,
                WithObservations = withObservations,
                InProgress = inProgress,
                RejectedOverall = rejectedOverall,
                CompletionRate = completionRate,
                SuccessRateOverClosed = successRateOverClosed,
                AvgCycleDays = avgCycleDays,
                StaleInscriptionsCount = staleCount,
                UpcomingDefensesCount = upcomingDefensesCount,
                ByModality = byModality,
                ByState = byState,
                ByLifecycleStatus = byLifecycleStatus,
                ByAcademicPeriod = byPeriod,
                ByProgram = byProgram,
                ByFaculty = byFaculty,
                Timeline = timeline,
                ModalityByState = modalityByState,
                ModalityByLifecycle = modalityByLifecycle,
                PeriodByModality = periodByModality,
                ByStudentCount = byStudentCount,
                ByTeacherLoad = byTeacherLoad,
                TeacherLoadDetailed = teacherLoadDetailed,
                ByAssignmentType = byAssignmentType
            };
        }

        public async Task<DashboardFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
        {
            var modalitiesRaw = await _context.Set<Modality>()
                .AsNoTracking()
                .Where(m => m.StatusRegister)
                .OrderBy(m => m.Name)
                .Select(m => new { m.Code, m.Name })
                .ToListAsync(cancellationToken);
            var modalities = modalitiesRaw.Select(m => new FilterOption(m.Code, m.Name)).ToList();

            var statesRaw = await _context.Set<StateInscription>()
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Select(s => new { s.Code, s.Name })
                .ToListAsync(cancellationToken);
            var states = statesRaw
                .Select(s => new FilterOption(s.Code, FormatStateInscriptionLabel(s.Code, s.Name)))
                .ToList();

            var periodsRaw = await _context.Set<AcademicPeriod>()
                .AsNoTracking()
                .Where(p => p.StatusRegister)
                .OrderByDescending(p => p.Code)
                .Select(p => new { p.Id, p.Code })
                .ToListAsync(cancellationToken);
            var periods = periodsRaw.Select(p => new FilterOption(p.Id.ToString(), p.Code)).ToList();

            var programsRaw = await _context.Set<AcademicProgram>()
                .AsNoTracking()
                .Where(p => p.StatusRegister)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);
            var programs = programsRaw.Select(p => new FilterOption(p.Id.ToString(), p.Name)).ToList();

            var facultiesRaw = await _context.Set<Faculty>()
                .AsNoTracking()
                .Where(f => f.StatusRegister)
                .OrderBy(f => f.Name)
                .Select(f => new { f.Id, f.Name })
                .ToListAsync(cancellationToken);
            var faculties = facultiesRaw.Select(f => new FilterOption(f.Id.ToString(), f.Name)).ToList();

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

        private async Task<List<ModalityFilterMeta>> BuildModalityFiltersAsync(CancellationToken cancellationToken)
        {
            var modalities = await _context.Set<Modality>()
                .AsNoTracking()
                .Where(m => m.StatusRegister)
                .Select(m => new { m.Id, m.Code, m.Name })
                .ToListAsync(cancellationToken);

            var stages = await _context.Set<StageModality>()
                .AsNoTracking()
                .Where(s => s.StatusRegister)
                .Select(s => new { s.IdModality, s.StageOrder, s.Code, s.Name })
                .ToListAsync(cancellationToken);

            var stateStages = await _context.Set<StateStage>()
                .AsNoTracking()
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

            var journalCategories = await _context.Set<ScientificArticle>()
                .AsNoTracking()
                .Where(a => a.StatusRegister && a.JournalCategory != null && a.JournalCategory != "")
                .Select(a => a.JournalCategory!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            var resultQuintiles = await _context.Set<SaberPro>()
                .AsNoTracking()
                .Where(s => s.StatusRegister && s.ResultQuintile != null && s.ResultQuintile != "")
                .Select(s => s.ResultQuintile!)
                .Distinct()
                .OrderBy(q => q)
                .ToListAsync(cancellationToken);

            var researchLinesRaw = await _context.Set<ResearchLine>()
                .AsNoTracking()
                .Where(r => r.StatusRegister)
                .OrderBy(r => r.Name)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync(cancellationToken);
            var researchLines = researchLinesRaw.Select(r => new FilterOption(r.Id.ToString(), r.Name)).ToList();

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
            if (filter.ModalityCodes is not { Count: 1 })
                return null;

            var modalityCode = filter.ModalityCodes[0];
            var baseQuery = BuildBaseQuery(filter);

            var modality = await _context.Set<Modality>()
                .AsNoTracking()
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

            var phaseRaw = await baseQuery
                .Where(i => i.StageModality != null)
                .GroupBy(i => new { i.StageModality!.StageOrder, i.StageModality.Code, i.StageModality.Name })
                .Select(g => new { g.Key.StageOrder, g.Key.Code, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);
            breakdown.PhaseFunnel = phaseRaw
                .OrderBy(p => p.StageOrder)
                .Select(p => new PhasePoint(p.StageOrder, p.Code, p.Name, p.Count))
                .ToList();

            // Distribución por estado de fase EFECTIVO (liviana: solo resuelve estado de la modalidad seleccionada).
            breakdown.ByStateStage = await GetEffectiveStateStageDistributionAsync(modalityCode, baseQuery, cancellationToken);

            await FillModalitySpecificAsync(modalityCode, baseQuery, breakdown, cancellationToken);

            if (modalityCode is "PROYECTO_GRADO" or "PRACTICA_ACADEMICA")
            {
                var teacherScope = filter.ActiveTeachersOnly ? FilterActiveOnly(baseQuery) : baseQuery;

                var assignTypeRaw = await teacherScope
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

        private static async Task<List<CategoryCount>> GetEffectiveStateStageDistributionAsync(
            string modalityCode, IQueryable<InscriptionModality> baseQuery, CancellationToken ct)
        {
            IQueryable<CodeNamePair>? q = modalityCode switch
            {
                "PROYECTO_GRADO" => baseQuery.Select(i => new CodeNamePair
                {
                    Code = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                        ? i.ProjectPresentation.StateStage.Code
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.Code
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.Code
                                : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                    ? i.Proposal.StateStage.Code
                                    : "",
                    Name = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                        ? i.ProjectPresentation.StateStage.Name
                        : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                            ? i.ProjectFinal.StateStage.Name
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.Name
                                : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                    ? i.Proposal.StateStage.Name
                                    : ""
                }),
                "PUBLICACION_ARTICULO" => baseQuery
                    .Where(i => i.ScientificArticle != null && i.ScientificArticle.StateStage != null)
                    .Select(i => new CodeNamePair { Code = i.ScientificArticle!.StateStage.Code, Name = i.ScientificArticle.StateStage.Name }),
                "PRACTICA_ACADEMICA" => baseQuery
                    .Where(i => i.AcademicPractice != null && i.AcademicPractice.StateStage != null)
                    .Select(i => new CodeNamePair { Code = i.AcademicPractice!.StateStage.Code, Name = i.AcademicPractice.StateStage.Name }),
                "COTERMINAL" => baseQuery
                    .Where(i => i.CoTerminal != null && i.CoTerminal.StateStage != null)
                    .Select(i => new CodeNamePair { Code = i.CoTerminal!.StateStage.Code, Name = i.CoTerminal.StateStage.Name }),
                "SEMINARIO_ACT" => baseQuery
                    .Where(i => i.Seminar != null && i.Seminar.StateStage != null)
                    .Select(i => new CodeNamePair { Code = i.Seminar!.StateStage.Code, Name = i.Seminar.StateStage.Name }),
                "GRADO_PROMEDIO" => baseQuery
                    .Where(i => i.AcademicAverage != null && i.AcademicAverage.StateStage != null)
                    .Select(i => new CodeNamePair { Code = i.AcademicAverage!.StateStage.Code, Name = i.AcademicAverage.StateStage.Name }),
                "SABER_PRO" => baseQuery
                    .Where(i => i.SaberPro != null && i.SaberPro.StateStage != null)
                    .Select(i => new CodeNamePair { Code = i.SaberPro!.StateStage.Code, Name = i.SaberPro.StateStage.Name }),
                _ => null
            };

            if (q == null)
                return new List<CategoryCount>();

            var raw = await q
                .Where(x => x.Code != "")
                .GroupBy(x => new { x.Code, x.Name })
                .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            return raw.Select(x => new CategoryCount(x.Code, x.Name, x.Count)).ToList();
        }

        private async Task FillModalitySpecificAsync(
            string modalityCode, IQueryable<InscriptionModality> baseQuery, ModalityBreakdown b, CancellationToken ct)
        {
            switch (modalityCode)
            {
                case "PUBLICACION_ARTICULO":
                {
                    b.DistributionLabel = "Categoría de revista";
                    var artRaw = await baseQuery.Where(i => i.ScientificArticle != null)
                        .GroupBy(i => i.ScientificArticle!.JournalCategory)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = artRaw
                        .Select(x => new CategoryCount(x.Key ?? "", string.IsNullOrWhiteSpace(x.Key) ? "Sin clasificar" : x.Key, x.Count))
                        .OrderByDescending(c => c.Count)
                        .ToList();

                    b.TopEntitiesLabel = "Top revistas de publicación";
                    var topJournals = await baseQuery
                        .Where(i => i.ScientificArticle != null && i.ScientificArticle.JournalName != null && i.ScientificArticle.JournalName != "")
                        .GroupBy(i => i.ScientificArticle!.JournalName!)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToListAsync(ct);
                    b.TopEntities = topJournals.Select(x => new CategoryCount(x.Name, x.Name, x.Count)).ToList();
                    break;
                }

                case "SABER_PRO":
                {
                    b.DistributionLabel = "Quintil de resultado";
                    var spRaw = await baseQuery.Where(i => i.SaberPro != null)
                        .GroupBy(i => i.SaberPro!.ResultQuintile)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToListAsync(ct);
                    b.Distribution = spRaw
                        .Select(x => new CategoryCount(x.Key ?? "", string.IsNullOrWhiteSpace(x.Key) ? "Sin dato" : x.Key, x.Count))
                        .OrderBy(c => c.Key)
                        .ToList();

                    var scoresQuery = baseQuery
                        .Where(i => i.SaberPro != null && i.SaberPro.ResultScore != null)
                        .Select(i => i.SaberPro!.ResultScore!.Value);
                    b.NumericLabel = "Puntaje Saber Pro";
                    b.NumericStats = await ComputeNumericStatsAsync(scoresQuery, "pts", ct);
                    b.NumericBuckets = await ComputeBucketsAsync(
                        scoresQuery,
                        new[] { "< 150", "150-169", "170-189", "190-209", ">= 210" },
                        v => v < 150m ? 0 : v < 170m ? 1 : v < 190m ? 2 : v < 210m ? 3 : 4,
                        ct);
                    break;
                }

                case "GRADO_PROMEDIO":
                {
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

                    var avgQuery = baseQuery
                        .Where(i => i.AcademicAverage != null && i.AcademicAverage.CertifiedAverage != null)
                        .Select(i => i.AcademicAverage!.CertifiedAverage!.Value);
                    b.NumericLabel = "Promedio certificado";
                    b.NumericStats = await ComputeNumericStatsAsync(avgQuery, "", ct);
                    b.NumericBuckets = await ComputeBucketsAsync(
                        avgQuery,
                        new[] { "< 4.5", "4.5-4.59", "4.6-4.69", "4.7-4.79", "4.8-5.0" },
                        v => v < 4.5m ? 0 : v < 4.6m ? 1 : v < 4.7m ? 2 : v < 4.8m ? 3 : 4,
                        ct);
                    break;
                }

                case "PRACTICA_ACADEMICA":
                {
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

                    var hoursQuery = baseQuery
                        .Where(i => i.AcademicPractice != null && i.AcademicPractice.PracticeHours != null)
                        .Select(i => (decimal)i.AcademicPractice!.PracticeHours!.Value);
                    b.NumericLabel = "Horas de práctica";
                    b.NumericStats = await ComputeNumericStatsAsync(hoursQuery, "horas", ct);
                    b.NumericBuckets = await ComputeBucketsAsync(
                        hoursQuery,
                        new[] { "< 640", "640", "641-720", "721-960", "> 960" },
                        v => v < 640m ? 0 : v == 640m ? 1 : v <= 720m ? 2 : v <= 960m ? 3 : 4,
                        ct);

                    b.TopEntitiesLabel = "Top entidades / empresas receptoras";
                    var topInstitutions = await baseQuery
                        .Where(i => i.AcademicPractice != null && i.AcademicPractice.InstitutionName != null && i.AcademicPractice.InstitutionName != "")
                        .GroupBy(i => i.AcademicPractice!.InstitutionName!)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToListAsync(ct);
                    b.TopEntities = topInstitutions.Select(x => new CategoryCount(x.Name, x.Name, x.Count)).ToList();
                    break;
                }

                case "COTERMINAL":
                {
                    var ctAvgQuery = baseQuery
                        .Where(i => i.CoTerminal != null && i.CoTerminal.FirstSemesterAverage != null)
                        .Select(i => i.CoTerminal!.FirstSemesterAverage!.Value);
                    b.NumericLabel = "Promedio 1er semestre";
                    b.NumericStats = await ComputeNumericStatsAsync(ctAvgQuery, "", ct);
                    b.NumericBuckets = await ComputeBucketsAsync(
                        ctAvgQuery,
                        new[] { "< 3.7", "3.7-3.99", "4.0-4.29", "4.3-4.59", ">= 4.6" },
                        v => v < 3.7m ? 0 : v < 4.0m ? 1 : v < 4.3m ? 2 : v < 4.6m ? 3 : 4,
                        ct);

                    b.TopEntitiesLabel = "Top programas / universidades Co-Terminal";
                    var topCt = await baseQuery
                        .Where(i => i.CoTerminal != null &&
                            ((i.CoTerminal.UniversityName != null && i.CoTerminal.UniversityName != "")
                             || (i.CoTerminal.PostgraduateProgramName != null && i.CoTerminal.PostgraduateProgramName != "")))
                        .GroupBy(i => new { i.CoTerminal!.UniversityName, i.CoTerminal.PostgraduateProgramName })
                        .Select(g => new { g.Key.UniversityName, g.Key.PostgraduateProgramName, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToListAsync(ct);
                    b.TopEntities = topCt.Select(x =>
                    {
                        var label = !string.IsNullOrWhiteSpace(x.PostgraduateProgramName) && !string.IsNullOrWhiteSpace(x.UniversityName)
                            ? $"{x.PostgraduateProgramName} — {x.UniversityName}"
                            : (x.PostgraduateProgramName ?? x.UniversityName ?? "");
                        return new CategoryCount(label, label, x.Count);
                    }).ToList();
                    break;
                }

                case "SEMINARIO_ACT":
                {
                    var gradeQuery = baseQuery
                        .Where(i => i.Seminar != null && i.Seminar.FinalGrade != null)
                        .Select(i => i.Seminar!.FinalGrade!.Value);
                    b.NumericLabel = "Nota final seminario";
                    b.NumericStats = await ComputeNumericStatsAsync(gradeQuery, "", ct);
                    b.NumericBuckets = await ComputeBucketsAsync(
                        gradeQuery,
                        new[] { "< 4.0", "4.0-4.29", "4.3-4.59", ">= 4.6" },
                        v => v < 4.0m ? 0 : v < 4.3m ? 1 : v < 4.6m ? 2 : 3,
                        ct);

                    b.TopEntitiesLabel = "Seminarios más cursados";
                    var topSeminars = await baseQuery
                        .Where(i => i.Seminar != null && i.Seminar.SeminarName != null && i.Seminar.SeminarName != "")
                        .GroupBy(i => i.Seminar!.SeminarName!)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToListAsync(ct);
                    b.TopEntities = topSeminars.Select(x => new CategoryCount(x.Name, x.Name, x.Count)).ToList();
                    break;
                }

                case "PROYECTO_GRADO":
                {
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

                    b.TopEntitiesLabel = "Top sublíneas de investigación";
                    var topSubLines = await baseQuery
                        .Where(i => i.Proposal != null && i.Proposal.ResearchSubLine != null)
                        .GroupBy(i => i.Proposal!.ResearchSubLine.Name)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToListAsync(ct);
                    b.TopEntities = topSubLines.Select(x => new CategoryCount(x.Name, x.Name, x.Count)).ToList();

                    var defenseRaw = await baseQuery
                        .Where(i => i.ProjectPresentation != null
                            && i.ProjectPresentation.StatusRegister
                            && i.ProjectPresentation.StateStage != null)
                        .GroupBy(i => new
                        {
                            StateCode = i.ProjectPresentation!.StateStage!.Code,
                            i.ProjectPresentation.AttemptNumber,
                            PresentationModality = i.ProjectPresentation.PresentationModality ?? "",
                            HasSchedule = i.ProjectPresentation.ScheduledDateTime != null
                        })
                        .Select(g => new
                        {
                            g.Key.StateCode,
                            g.Key.AttemptNumber,
                            g.Key.PresentationModality,
                            g.Key.HasSchedule,
                            Count = g.Count()
                        })
                        .ToListAsync(ct);

                    var totalScheduled = defenseRaw
                        .Where(x => x.HasSchedule || x.StateCode == "SUST_PROGRAMADA")
                        .Sum(x => x.Count);
                    var approvedFirst = defenseRaw
                        .Where(x => x.StateCode == "SUST_APROBADA" && x.AttemptNumber <= 1)
                        .Sum(x => x.Count);
                    var approvedSecond = defenseRaw
                        .Where(x => x.StateCode == "SUST_APROBADA" && x.AttemptNumber >= 2)
                        .Sum(x => x.Count);
                    var retryPending = defenseRaw
                        .Where(x => x.StateCode == "SUST_NO_APROBADA_REINTENTO")
                        .Sum(x => x.Count);
                    var rejectedFinal = defenseRaw
                        .Where(x => x.StateCode == "SUST_REPROBADA_FINAL")
                        .Sum(x => x.Count);

                    var byPresModality = defenseRaw
                        .Where(x => !string.IsNullOrWhiteSpace(x.PresentationModality))
                        .GroupBy(x => x.PresentationModality.Trim().ToUpperInvariant())
                        .Select(g => new CategoryCount(g.Key, FormatDefenseModalityLabel(g.Key), g.Sum(x => x.Count)))
                        .OrderByDescending(c => c.Count)
                        .ToList();

                    b.DefenseSummary = new DefenseMetricsSummary(
                        totalScheduled,
                        approvedFirst,
                        approvedSecond,
                        retryPending,
                        rejectedFinal,
                        byPresModality);
                    break;
                }
            }
        }

        private static string FormatDefenseModalityLabel(string code) => code switch
        {
            "PRESENCIAL" => "Presencial",
            "VIRTUAL" => "Virtual",
            "MIXTA" => "Mixta",
            _ => code
        };

        private static async Task<NumericSummaryStats?> ComputeNumericStatsAsync(
            IQueryable<decimal> valuesQuery, string unit, CancellationToken ct)
        {
            var stats = await valuesQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Min = g.Min(),
                    Avg = g.Average(),
                    Max = g.Max()
                })
                .FirstOrDefaultAsync(ct);

            if (stats == null || stats.Count == 0)
                return null;

            decimal median;
            if (stats.Count % 2 == 1)
            {
                median = await valuesQuery
                    .OrderBy(v => v)
                    .Skip(stats.Count / 2)
                    .FirstAsync(ct);
            }
            else
            {
                var midPair = await valuesQuery
                    .OrderBy(v => v)
                    .Skip((stats.Count / 2) - 1)
                    .Take(2)
                    .ToListAsync(ct);
                median = midPair.Count == 2 ? (midPair[0] + midPair[1]) / 2m : midPair[0];
            }

            return new NumericSummaryStats(
                stats.Count,
                Math.Round(stats.Min, 2),
                Math.Round(stats.Avg, 2),
                Math.Round(median, 2),
                Math.Round(stats.Max, 2),
                unit);
        }

        private static async Task<List<CategoryCount>> ComputeBucketsAsync(
            IQueryable<decimal> valuesQuery,
            string[] labels,
            System.Linq.Expressions.Expression<Func<decimal, int>> bucketSelector,
            CancellationToken ct)
        {
            var raw = await valuesQuery
                .Select(bucketSelector)
                .GroupBy(b => b)
                .Select(g => new { Bucket = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            if (raw.Count == 0)
                return new List<CategoryCount>();

            var map = raw.ToDictionary(x => x.Bucket, x => x.Count);
            var buckets = new List<CategoryCount>(labels.Length);
            for (var idx = 0; idx < labels.Length; idx++)
            {
                var count = map.TryGetValue(idx, out var c) ? c : 0;
                buckets.Add(new CategoryCount(labels[idx], labels[idx], count));
            }
            return buckets;
        }

        public async Task<DashboardOperationalAlerts> GetOperationalAlertsAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var threshold = filter.StaleDaysThreshold > 0 ? filter.StaleDaysThreshold : 30;
            var staleCutoffUtc = nowUtc.AddDays(-threshold);

            var slaConfig = await _context.SlaAlertConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == 1 && x.StatusRegister, cancellationToken);
            var evaluatorOverdueBusinessDays = slaConfig?.EvaluatorOverdueBusinessDays > 0
                ? slaConfig.EvaluatorOverdueBusinessDays
                : 25;
            var slaCutoffExclusiveUtc = DashboardTimeHelper.GetBusinessDaysCutoffExclusiveUtc(
                nowUtc,
                evaluatorOverdueBusinessDays);

            var baseQuery = BuildBaseQuery(filter);
            var lifecycleQuery = WithLifecycle(ProjectInscriptions(baseQuery));

            // 1. Inscripciones estancadas + incumplimiento SLA Acuerdo 015.
            var staleBase = lifecycleQuery
                .Where(x => x.LifecycleCode != LifecycleCompleted
                    && x.LifecycleCode != LifecycleRejected
                    && x.LastActivityAt <= staleCutoffUtc);

            var totalStaleCount = await staleBase.CountAsync(cancellationToken);

            var totalSlaBreachCount = await lifecycleQuery
                .CountAsync(x => x.LifecycleCode == LifecycleInReview
                    && JuryEvaluationStateStageCodes.Contains(x.EffectiveStateStageCode)
                    && x.LastActivityAt < slaCutoffExclusiveUtc, cancellationToken);

            var staleRaw = await staleBase
                .OrderBy(x => x.LastActivityAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            // 2. Riesgo de vencimiento de plazo (Modality.MaximumTermPeriods).
            var termRiskBase = lifecycleQuery
                .Where(x => x.LifecycleCode != LifecycleCompleted
                    && x.LifecycleCode != LifecycleRejected
                    && x.MaximumTermPeriods != null
                    && x.MaximumTermPeriods > 0
                    && x.PeriodStartDate != null)
                .Select(x => new
                {
                    x.Id,
                    x.ModalityCode,
                    x.ModalityName,
                    x.PeriodCode,
                    PeriodStartDate = x.PeriodStartDate!.Value,
                    MaxTermPeriods = x.MaximumTermPeriods!.Value,
                    x.StageName,
                    x.EffectiveStateStageName,
                    RawPeriodsElapsed = _context.Set<AcademicPeriod>()
                        .Count(ap => ap.StatusRegister && ap.StartDate >= x.PeriodStartDate!.Value && ap.StartDate <= nowUtc)
                })
                .Select(x => new
                {
                    x.Id,
                    x.ModalityCode,
                    x.ModalityName,
                    x.PeriodCode,
                    x.PeriodStartDate,
                    x.MaxTermPeriods,
                    x.StageName,
                    x.EffectiveStateStageName,
                    PeriodsElapsed = x.RawPeriodsElapsed < 1 ? 1 : x.RawPeriodsElapsed
                })
                .Where(x => x.PeriodsElapsed >= x.MaxTermPeriods
                    || (x.MaxTermPeriods >= 3 && x.PeriodsElapsed == x.MaxTermPeriods - 1));

            var totalTermRiskCount = await termRiskBase.CountAsync(cancellationToken);

            var termRiskRaw = await termRiskBase
                .OrderByDescending(x => x.PeriodsElapsed - x.MaxTermPeriods)
                .ThenBy(x => x.PeriodStartDate)
                .Take(50)
                .ToListAsync(cancellationToken);

            // 3. Próximas sustentaciones programadas.
            var upcomingRaw = await baseQuery
                .Where(i => !i.IsFinalRejection
                    && (i.StateInscription == null || i.StateInscription.Code != "RECHAZADO")
                    && i.ProjectPresentation != null
                    && i.ProjectPresentation.StatusRegister
                    && i.ProjectPresentation.StateStage != null
                    && i.ProjectPresentation.StateStage.Code == "SUST_PROGRAMADA"
                    && i.ProjectPresentation.ScheduledDateTime != null
                    && i.ProjectPresentation.ScheduledDateTime >= nowUtc)
                .OrderBy(i => i.ProjectPresentation!.ScheduledDateTime)
                .Take(30)
                .Select(i => new
                {
                    i.Id,
                    Title = i.Proposal != null ? i.Proposal.Title : "",
                    ScheduledDateTime = i.ProjectPresentation!.ScheduledDateTime!.Value,
                    PresentationModality = i.ProjectPresentation.PresentationModality ?? "",
                    LocationOrLink = i.ProjectPresentation.LocationOrLink ?? "",
                    i.ProjectPresentation.AttemptNumber
                })
                .ToListAsync(cancellationToken);

            var alertIds = staleRaw.Select(x => x.Id)
                .Concat(termRiskRaw.Select(x => x.Id))
                .Concat(upcomingRaw.Select(x => x.Id))
                .Distinct()
                .ToList();

            var studentsByInscription = new Dictionary<int, string>();
            if (alertIds.Count > 0)
            {
                var studentRows = await _context.Set<UserInscriptionModality>()
                    .AsNoTracking()
                    .Where(uim => uim.StatusRegister && uim.IsConfirmed && uim.User != null && alertIds.Contains(uim.IdInscriptionModality))
                    .Select(uim => new
                    {
                        uim.IdInscriptionModality,
                        FullName = (uim.User!.FirstName + " " + uim.User.LastName).Trim()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var g in studentRows.GroupBy(s => s.IdInscriptionModality))
                    studentsByInscription[g.Key] = string.Join("; ", g.Select(x => x.FullName).Where(n => !string.IsNullOrWhiteSpace(n)));
            }

            var upcomingIds = upcomingRaw.Select(x => x.Id).Distinct().ToList();
            var juriesByInscription = new Dictionary<int, string>();
            if (upcomingIds.Count > 0)
            {
                var juryRows = await _context.Set<TeachingAssignment>()
                    .AsNoTracking()
                    .Where(ta => ta.StatusRegister
                        && ta.RevocationDate == null
                        && ta.Teacher != null
                        && ta.TypeTeachingAssignment != null
                        && upcomingIds.Contains(ta.IdInscriptionModality)
                        && (ta.TypeTeachingAssignment.Code == "JURADO_EVALUADOR"
                            || ta.TypeTeachingAssignment.Code.ToUpper().Contains("JURADO")
                            || ta.TypeTeachingAssignment.Name.ToLower().Contains("jurado")))
                    .Select(ta => new
                    {
                        ta.IdInscriptionModality,
                        FullName = (ta.Teacher!.FirstName + " " + ta.Teacher.LastName).Trim()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var g in juryRows.GroupBy(j => j.IdInscriptionModality))
                    juriesByInscription[g.Key] = string.Join("; ", g.Select(x => x.FullName).Where(n => !string.IsNullOrWhiteSpace(n)));
            }

            var staleItems = staleRaw.Select(x =>
            {
                var daysSince = Math.Max(0, (int)(nowUtc - x.LastActivityAt).TotalDays);
                var isJuryEval = JuryEvaluationStateStageCodes.Contains(x.EffectiveStateStageCode);
                var slaBreach = x.LifecycleCode == LifecycleInReview
                    && isJuryEval
                    && DashboardTimeHelper.CountBusinessDaysElapsed(x.LastActivityAt, nowUtc) >= evaluatorOverdueBusinessDays;
                var responsible = isJuryEval
                    ? "Jurados / Evaluadores"
                    : x.LifecycleCode == LifecycleInReview
                        ? "Comité / Coordinación"
                        : "Estudiante";

                return new StaleInscriptionAlert(
                    x.Id,
                    x.ModalityCode,
                    x.ModalityName,
                    studentsByInscription.TryGetValue(x.Id, out var st) ? st : "",
                    x.StageName,
                    x.EffectiveStateStageName,
                    x.LifecycleCode,
                    GetLifecycleLabel(x.LifecycleCode),
                    daysSince,
                    DashboardTimeHelper.FormatDateTime(x.LastActivityAt),
                    slaBreach,
                    responsible);
            }).ToList();

            var termRiskItems = termRiskRaw.Select(x => new TermRiskAlert(
                x.Id,
                x.ModalityCode,
                x.ModalityName,
                studentsByInscription.TryGetValue(x.Id, out var st) ? st : "",
                x.PeriodCode,
                x.PeriodsElapsed,
                x.MaxTermPeriods,
                x.PeriodsElapsed > x.MaxTermPeriods ? "EXCEEDED" : "WARNING",
                x.StageName,
                x.EffectiveStateStageName)).ToList();

            var upcomingDefenses = upcomingRaw.Select(x => new UpcomingDefenseAlert(
                x.Id,
                x.Title,
                studentsByInscription.TryGetValue(x.Id, out var st) ? st : "",
                DashboardTimeHelper.FormatDateTime(x.ScheduledDateTime),
                string.IsNullOrWhiteSpace(x.PresentationModality) ? "Sin definir" : FormatDefenseModalityLabel(x.PresentationModality.Trim().ToUpperInvariant()),
                x.LocationOrLink,
                x.AttemptNumber,
                juriesByInscription.TryGetValue(x.Id, out var jr) ? jr : "")).ToList();

            // 4. Retrabajo por modalidad (INNER JOIN + GROUP BY en SQL).
            var modalityTotals = await baseQuery
                .Where(i => i.Modality != null)
                .GroupBy(i => new { i.Modality!.Code, i.Modality.Name })
                .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var baseIdModality = baseQuery
                .Where(i => i.Modality != null)
                .Select(i => new { i.Id, ModalityCode = i.Modality!.Code });

            var obsEvalRaw = await _context.Set<Evaluation>()
                .AsNoTracking()
                .Where(e => e.StatusRegister && e.Result != null &&
                    (e.Result.ToUpper().Contains("OBSERV")
                     || e.Result.ToUpper().Contains("NO_PERTINENTE")
                     || e.Result.ToUpper().Contains("REINTENTO")))
                .Join(baseIdModality,
                    e => e.EntityId,
                    i => i.Id,
                    (e, i) => i.ModalityCode)
                .GroupBy(code => code)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var obsMap = obsEvalRaw.ToDictionary(x => x.Code, x => x.Count);

            var replacedDocsRaw = await _context.Set<Document>()
                .AsNoTracking()
                .Where(d => d.StatusRegister && d.IdDocumentOld != null && d.IdInscriptionModality != null)
                .Join(baseIdModality,
                    d => d.IdInscriptionModality!.Value,
                    i => i.Id,
                    (d, i) => i.ModalityCode)
                .GroupBy(code => code)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var replacedDocsMap = replacedDocsRaw.ToDictionary(x => x.Code, x => x.Count);

            var reworkByModality = modalityTotals
                .Select(m =>
                {
                    var obsCount = obsMap.TryGetValue(m.Code, out var o) ? o : 0;
                    var docsCount = replacedDocsMap.TryGetValue(m.Code, out var d) ? d : 0;
                    var avgObs = m.Count > 0 ? Math.Round((double)obsCount / m.Count, 2) : 0.0;
                    return new ReworkModalityMetric(m.Code, m.Name, m.Count, obsCount, avgObs, docsCount);
                })
                .OrderByDescending(x => x.ObservationEvaluations)
                .ThenByDescending(x => x.ReplacedDocumentsCount)
                .ThenBy(x => x.ModalityName)
                .ToList();

            return new DashboardOperationalAlerts
            {
                StaleThresholdDays = threshold,
                TotalStaleCount = totalStaleCount,
                TotalEvaluationSlaBreachCount = totalSlaBreachCount,
                TotalTermRiskCount = totalTermRiskCount,
                StaleItems = staleItems,
                TermRiskItems = termRiskItems,
                UpcomingDefenses = upcomingDefenses,
                ReworkByModality = reworkByModality
            };
        }

        public async Task<DashboardRecordsPage> GetPagedRecordsAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 200);
            var nowUtc = DateTimeOffset.UtcNow;

            var baseQuery = ApplySearch(BuildBaseQuery(filter), filter.Search);
            var totalCount = await baseQuery.CountAsync(cancellationToken);
            if (totalCount == 0)
                return new DashboardRecordsPage(new List<DashboardExportRow>(), 0, page, pageSize);

            var lifecycleQuery = WithLifecycle(ProjectInscriptions(baseQuery));
            var sorted = ApplySorting(lifecycleQuery, filter.SortBy, filter.SortDir, defaultNewestFirst: true);
            var pageProjections = await sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = await HydrateExportRowsAsync(pageProjections, nowUtc, cancellationToken);
            return new DashboardRecordsPage(items, totalCount, page, pageSize);
        }

        public async Task<List<DashboardExportRow>> GetExportRowsAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var baseQuery = ApplySearch(BuildBaseQuery(filter), filter.Search);
            var lifecycleQuery = WithLifecycle(ProjectInscriptions(baseQuery));

            var sorted = ApplySorting(lifecycleQuery, filter.SortBy, filter.SortDir, defaultNewestFirst: false);
            var projections = await sorted
                .Take(MaxExportRows)
                .ToListAsync(cancellationToken);

            return await HydrateExportRowsAsync(projections, nowUtc, cancellationToken);
        }

        private static IQueryable<InscriptionLifecycleProjection> ApplySorting(
            IQueryable<InscriptionLifecycleProjection> query,
            string? sortBy,
            string? sortDir,
            bool defaultNewestFirst)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return defaultNewestFirst
                    ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.Id);
            }

            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.Trim().ToLowerInvariant() switch
            {
                "id" or "inscriptionid" => desc ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
                "modality" or "modalityname" => desc
                    ? query.OrderByDescending(x => x.ModalityName).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.ModalityName).ThenBy(x => x.Id),
                "state" or "statename" => desc
                    ? query.OrderByDescending(x => x.StateName).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.StateName).ThenBy(x => x.Id),
                "lifecycle" or "lifecyclestatus" or "lifecyclecode" => desc
                    ? query.OrderByDescending(x => x.LifecycleCode).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.LifecycleCode).ThenBy(x => x.Id),
                "period" or "academicperiod" => desc
                    ? query.OrderByDescending(x => x.PeriodCode).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.PeriodCode).ThenBy(x => x.Id),
                "currentphase" or "phase" => desc
                    ? query.OrderByDescending(x => x.StageName).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.StageName).ThenBy(x => x.Id),
                "phasestate" => desc
                    ? query.OrderByDescending(x => x.EffectiveStateStageName).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.EffectiveStateStageName).ThenBy(x => x.Id),
                "approvaldate" => desc
                    ? query.OrderByDescending(x => x.ApprovalDate).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.ApprovalDate).ThenBy(x => x.Id),
                "lastactivityat" => desc
                    ? query.OrderByDescending(x => x.LastActivityAt).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.LastActivityAt).ThenBy(x => x.Id),
                "dayselapsed" or "dayssinceactivity" => desc
                    ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
                _ => desc
                    ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            };
        }

        private async Task<List<DashboardExportRow>> HydrateExportRowsAsync(
            List<InscriptionLifecycleProjection> projections,
            DateTimeOffset nowUtc,
            CancellationToken ct)
        {
            if (projections.Count == 0)
                return new List<DashboardExportRow>();

            var allIds = projections.Select(p => p.Id).ToList();
            var studentsById = new Dictionary<int, List<StudentDetailRow>>();
            var teachersById = new Dictionary<int, List<string>>();
            var detailById = new Dictionary<int, ModalityExtensionDetail>();

            foreach (var chunk in allIds.Chunk(HydrationChunkSize))
            {
                var chunkIds = chunk.ToList();

                var studentRows = await _context.Set<UserInscriptionModality>()
                    .AsNoTracking()
                    .Where(uim => uim.StatusRegister && uim.IsConfirmed && uim.User != null && chunkIds.Contains(uim.IdInscriptionModality))
                    .Select(uim => new StudentDetailRow
                    {
                        IdInscriptionModality = uim.IdInscriptionModality,
                        FullName = (uim.User!.FirstName + " " + uim.User.LastName).Trim(),
                        Identification = uim.User.Identification,
                        Email = uim.User.Email,
                        Program = uim.User.AcademicProgram != null ? uim.User.AcademicProgram.Name : "",
                        Faculty = uim.User.AcademicProgram != null && uim.User.AcademicProgram.Faculty != null
                            ? uim.User.AcademicProgram.Faculty.Name
                            : ""
                    })
                    .ToListAsync(ct);

                foreach (var s in studentRows)
                {
                    if (!studentsById.TryGetValue(s.IdInscriptionModality, out var list))
                    {
                        list = new List<StudentDetailRow>();
                        studentsById[s.IdInscriptionModality] = list;
                    }
                    list.Add(s);
                }

                var teacherRows = await _context.Set<TeachingAssignment>()
                    .AsNoTracking()
                    .Where(ta => ta.StatusRegister && ta.RevocationDate == null && ta.Teacher != null && chunkIds.Contains(ta.IdInscriptionModality))
                    .Select(ta => new
                    {
                        ta.IdInscriptionModality,
                        FullName = (ta.Teacher!.FirstName + " " + ta.Teacher.LastName).Trim(),
                        Role = ta.TypeTeachingAssignment != null ? ta.TypeTeachingAssignment.Name : ""
                    })
                    .ToListAsync(ct);

                foreach (var t in teacherRows)
                {
                    if (!teachersById.TryGetValue(t.IdInscriptionModality, out var list))
                    {
                        list = new List<string>();
                        teachersById[t.IdInscriptionModality] = list;
                    }
                    list.Add(string.IsNullOrWhiteSpace(t.Role) ? t.FullName : $"{t.FullName} ({t.Role})");
                }

                var extRows = await _context.Set<InscriptionModality>()
                    .AsNoTracking()
                    .Where(i => chunkIds.Contains(i.Id))
                    .Select(i => new ModalityExtensionDetail
                    {
                        Id = i.Id,
                        ProposalTitle = i.Proposal != null ? i.Proposal.Title : null,
                        ResearchLine = i.Proposal != null && i.Proposal.ResearchLine != null ? i.Proposal.ResearchLine.Name : null,
                        ResearchSubLine = i.Proposal != null && i.Proposal.ResearchSubLine != null ? i.Proposal.ResearchSubLine.Name : null,

                        PracticeTitle = i.AcademicPractice != null ? i.AcademicPractice.Title : null,
                        PracticeIsEmprendimiento = i.AcademicPractice != null ? (bool?)i.AcademicPractice.IsEmprendimiento : null,
                        PracticeInstitution = i.AcademicPractice != null ? i.AcademicPractice.InstitutionName : null,
                        PracticeHours = i.AcademicPractice != null ? i.AcademicPractice.PracticeHours : null,

                        ArticleTitle = i.ScientificArticle != null ? i.ScientificArticle.ArticleTitle : null,
                        JournalCategory = i.ScientificArticle != null ? i.ScientificArticle.JournalCategory : null,
                        JournalName = i.ScientificArticle != null ? i.ScientificArticle.JournalName : null,
                        Issn = i.ScientificArticle != null ? i.ScientificArticle.ISSN : null,

                        CoTerminalUniversity = i.CoTerminal != null ? i.CoTerminal.UniversityName : null,
                        CoTerminalProgram = i.CoTerminal != null ? i.CoTerminal.PostgraduateProgramName : null,
                        CoTerminalAvg = i.CoTerminal != null ? i.CoTerminal.FirstSemesterAverage : null,

                        SeminarName = i.Seminar != null ? i.Seminar.SeminarName : null,
                        SeminarGrade = i.Seminar != null ? i.Seminar.FinalGrade : null,
                        SeminarAttendance = i.Seminar != null ? i.Seminar.AttendancePercentage : null,

                        CertifiedAverage = i.AcademicAverage != null ? i.AcademicAverage.CertifiedAverage : null,
                        HasFailedSubjects = i.AcademicAverage != null ? i.AcademicAverage.HasFailedSubjects : null,

                        SaberProQuintile = i.SaberPro != null ? i.SaberPro.ResultQuintile : null,
                        SaberProScore = i.SaberPro != null ? i.SaberPro.ResultScore : null
                    })
                    .ToListAsync(ct);

                foreach (var ext in extRows)
                    detailById[ext.Id] = ext;
            }

            var result = new List<DashboardExportRow>(projections.Count);
            foreach (var p in projections)
            {
                studentsById.TryGetValue(p.Id, out var students);
                teachersById.TryGetValue(p.Id, out var teachers);
                detailById.TryGetValue(p.Id, out var ext);

                var studentNames = students != null
                    ? string.Join("; ", students.Select(s => s.FullName).Where(x => !string.IsNullOrWhiteSpace(x)))
                    : "";
                var studentIds = students != null
                    ? string.Join("; ", students.Select(s => s.Identification).Where(x => !string.IsNullOrWhiteSpace(x)))
                    : "";
                var studentEmails = students != null
                    ? string.Join("; ", students.Select(s => s.Email).Where(x => !string.IsNullOrWhiteSpace(x)))
                    : "";
                var program = students?.Select(s => s.Program).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
                var faculty = students?.Select(s => s.Faculty).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
                var teacherList = teachers != null ? string.Join("; ", teachers) : "";

                var (title, modalityDetail) = BuildTitleAndDetail(p.ModalityCode, ext);
                var referenceEnd = p.LifecycleCode == LifecycleCompleted ? p.CompletionDate : nowUtc;
                var daysElapsed = Math.Max(0, (int)(referenceEnd - p.CreatedAt).TotalDays);

                result.Add(new DashboardExportRow(
                    InscriptionId: p.Id,
                    ModalityName: p.ModalityName,
                    StateName: FormatStateInscriptionLabel(p.StateCode, p.StateName),
                    AcademicPeriod: p.PeriodCode,
                    Program: program,
                    Faculty: faculty,
                    Students: studentNames,
                    CreatedAt: DashboardTimeHelper.FormatDateTime(p.CreatedAt),
                    ApprovalDate: DashboardTimeHelper.FormatDate(p.ApprovalDate),
                    CurrentPhase: p.StageName,
                    PhaseState: p.EffectiveStateStageName,
                    ModalityDetail: modalityDetail,
                    ModalityCode: p.ModalityCode,
                    LifecycleCode: p.LifecycleCode,
                    LifecycleStatus: GetLifecycleLabel(p.LifecycleCode),
                    StudentIdentifications: studentIds,
                    StudentEmails: studentEmails,
                    Teachers: teacherList,
                    Title: title,
                    ResearchLine: ext?.ResearchLine ?? "",
                    ResearchSubLine: ext?.ResearchSubLine ?? "",
                    LastActivityAt: DashboardTimeHelper.FormatDateTime(p.LastActivityAt),
                    DaysElapsed: daysElapsed));
            }

            return result;
        }

        private static (string Title, string Detail) BuildTitleAndDetail(string modalityCode, ModalityExtensionDetail? ext)
        {
            if (ext == null)
                return ("", "");

            static string JoinParts(params string?[] parts) =>
                string.Join(" | ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));

            return modalityCode switch
            {
                "PROYECTO_GRADO" => (
                    ext.ProposalTitle ?? "",
                    JoinParts(ext.ProposalTitle, ext.ResearchLine, ext.ResearchSubLine)),

                "PRACTICA_ACADEMICA" => (
                    ext.PracticeTitle ?? "",
                    JoinParts(
                        ext.PracticeIsEmprendimiento.HasValue
                            ? (ext.PracticeIsEmprendimiento.Value ? "Emprendimiento" : "Práctica")
                            : null,
                        ext.PracticeInstitution,
                        ext.PracticeHours.HasValue ? $"{ext.PracticeHours.Value} h" : null)),

                "PUBLICACION_ARTICULO" => (
                    ext.ArticleTitle ?? "",
                    JoinParts(
                        ext.JournalCategory,
                        ext.JournalName,
                        !string.IsNullOrWhiteSpace(ext.Issn) ? $"ISSN: {ext.Issn}" : null)),

                "COTERMINAL" => (
                    ext.CoTerminalProgram ?? "",
                    JoinParts(
                        ext.CoTerminalUniversity,
                        ext.CoTerminalProgram,
                        ext.CoTerminalAvg.HasValue
                            ? $"Prom: {ext.CoTerminalAvg.Value.ToString("0.00", CultureInfo.InvariantCulture)}"
                            : null)),

                "SEMINARIO_ACT" => (
                    ext.SeminarName ?? "",
                    JoinParts(
                        ext.SeminarName,
                        ext.SeminarGrade.HasValue
                            ? $"Nota: {ext.SeminarGrade.Value.ToString("0.00", CultureInfo.InvariantCulture)}"
                            : null,
                        ext.SeminarAttendance.HasValue
                            ? $"Asist: {ext.SeminarAttendance.Value.ToString("0.#", CultureInfo.InvariantCulture)}%"
                            : null)),

                "GRADO_PROMEDIO" => (
                    "",
                    JoinParts(
                        ext.CertifiedAverage.HasValue
                            ? $"Promedio: {ext.CertifiedAverage.Value.ToString("0.00", CultureInfo.InvariantCulture)}"
                            : null,
                        ext.HasFailedSubjects.HasValue
                            ? (ext.HasFailedSubjects.Value ? "Con pérdidas" : "Sin pérdidas")
                            : null)),

                "SABER_PRO" => (
                    "",
                    JoinParts(
                        !string.IsNullOrWhiteSpace(ext.SaberProQuintile) ? $"Quintil: {ext.SaberProQuintile}" : null,
                        ext.SaberProScore.HasValue
                            ? $"Puntaje: {ext.SaberProScore.Value.ToString("0.##", CultureInfo.InvariantCulture)}"
                            : null)),

                _ => ("", "")
            };
        }

        private sealed class CodeNamePair
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
        }

        private sealed class LifecycleFlagProjection
        {
            public int Id { get; set; }
            public string StateCode { get; set; } = "";
            public bool IsFinalRejection { get; set; }
            public string EffectiveStateStageCode { get; set; } = "";
            public bool EffectiveIsFinalOverall { get; set; }
            public bool EffectiveIsRejectionState { get; set; }
        }

        private sealed class InscriptionProjection
        {
            public int Id { get; set; }
            public string ModalityCode { get; set; } = "";
            public string ModalityName { get; set; } = "";
            public int? MaximumTermPeriods { get; set; }
            public string StateCode { get; set; } = "";
            public string StateName { get; set; } = "";
            public bool IsFinalRejection { get; set; }
            public int IdAcademicPeriod { get; set; }
            public string PeriodCode { get; set; } = "";
            public DateTimeOffset? PeriodStartDate { get; set; }
            public int? StageOrder { get; set; }
            public string StageCode { get; set; } = "";
            public string StageName { get; set; } = "";
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
            public DateTimeOffset? ApprovalDate { get; set; }
            public string EffectiveStateStageCode { get; set; } = "";
            public string EffectiveStateStageName { get; set; } = "";
            public bool EffectiveIsFinalOverall { get; set; }
            public bool EffectiveIsRejectionState { get; set; }
            public DateTimeOffset ExtensionActivityAt { get; set; }
            public DateTimeOffset CompletionDate { get; set; }
        }

        private sealed class InscriptionLifecycleProjection
        {
            public int Id { get; set; }
            public string ModalityCode { get; set; } = "";
            public string ModalityName { get; set; } = "";
            public int? MaximumTermPeriods { get; set; }
            public string StateCode { get; set; } = "";
            public string StateName { get; set; } = "";
            public int IdAcademicPeriod { get; set; }
            public string PeriodCode { get; set; } = "";
            public DateTimeOffset? PeriodStartDate { get; set; }
            public int? StageOrder { get; set; }
            public string StageCode { get; set; } = "";
            public string StageName { get; set; } = "";
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? ApprovalDate { get; set; }
            public string EffectiveStateStageCode { get; set; } = "";
            public string EffectiveStateStageName { get; set; } = "";
            public DateTimeOffset LastActivityAt { get; set; }
            public DateTimeOffset CompletionDate { get; set; }
            public string LifecycleCode { get; set; } = "";
        }

        private sealed class StudentDetailRow
        {
            public int IdInscriptionModality { get; set; }
            public string FullName { get; set; } = "";
            public string Identification { get; set; } = "";
            public string Email { get; set; } = "";
            public string Program { get; set; } = "";
            public string Faculty { get; set; } = "";
        }

        private sealed class ModalityExtensionDetail
        {
            public int Id { get; set; }
            public string? ProposalTitle { get; set; }
            public string? ResearchLine { get; set; }
            public string? ResearchSubLine { get; set; }
            public string? PracticeTitle { get; set; }
            public bool? PracticeIsEmprendimiento { get; set; }
            public string? PracticeInstitution { get; set; }
            public int? PracticeHours { get; set; }
            public string? ArticleTitle { get; set; }
            public string? JournalCategory { get; set; }
            public string? JournalName { get; set; }
            public string? Issn { get; set; }
            public string? CoTerminalUniversity { get; set; }
            public string? CoTerminalProgram { get; set; }
            public decimal? CoTerminalAvg { get; set; }
            public string? SeminarName { get; set; }
            public decimal? SeminarGrade { get; set; }
            public decimal? SeminarAttendance { get; set; }
            public decimal? CertifiedAverage { get; set; }
            public bool? HasFailedSubjects { get; set; }
            public string? SaberProQuintile { get; set; }
            public decimal? SaberProScore { get; set; }
        }
    }
}
