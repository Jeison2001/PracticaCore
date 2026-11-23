using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class SlaAlertRepository : ISlaAlertRepository
    {
        private const int HydrationChunkSize = 2_000;

        private readonly AppDbContext _context;
        private readonly ILogger<SlaAlertRepository> _logger;

        public SlaAlertRepository(AppDbContext context, ILogger<SlaAlertRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<SlaAlertConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
            _context.SlaAlertConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == 1 && x.StatusRegister, cancellationToken);

        public async Task SaveConfigurationRunMetricsAsync(
            DateTimeOffset runAtUtc,
            SlaAlertExecutionSummary summary,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.SlaAlertConfigurations
                .FirstOrDefaultAsync(x => x.Id == 1 && x.StatusRegister, cancellationToken);

            if (entity == null)
                return;

            entity.LastRunAt = runAtUtc;
            entity.LastRunEvaluatedCount = summary.EvaluatedInscriptions;
            entity.LastRunTriggeredCount = summary.AlertsTriggered;
            entity.LastRunSkippedCount = summary.AlertsSkippedAlreadySent;
            entity.LastRunFailedCount = summary.AlertsFailed;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SlaAlertInscriptionCandidate>> GetActiveCandidatesAsync(
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            var rawRows = await _context.Set<InscriptionModality>()
                .AsNoTracking()
                .Where(i => i.StatusRegister
                    && !i.IsFinalRejection
                    && (i.StateInscription == null || i.StateInscription.Code != StateInscriptionCodes.Rechazado))
                .Select(i => new RawCandidateProjection
                {
                    Id = i.Id,
                    ModalityCode = i.Modality != null ? i.Modality.Code : "",
                    ModalityName = i.Modality != null ? i.Modality.Name : "",
                    MaximumTermPeriods = i.Modality != null ? i.Modality.MaximumTermPeriods : null,
                    StateInscriptionCode = i.StateInscription != null ? i.StateInscription.Code : "",
                    StateInscriptionName = i.StateInscription != null ? i.StateInscription.Name : "",
                    AcademicPeriodCode = i.AcademicPeriod != null ? i.AcademicPeriod.Code : "",
                    PeriodStartDate = i.AcademicPeriod != null ? (DateTimeOffset?)i.AcademicPeriod.StartDate : null,
                    StageOrder = i.StageModality != null ? (int?)i.StageModality.StageOrder : null,
                    StageCode = i.StageModality != null ? i.StageModality.Code : "",
                    StageName = i.StageModality != null ? i.StageModality.Name : "",
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    InscriptionObservations = i.Observations,

                    EffectiveStateStageCode =
                        i.Modality != null && i.Modality.Code == ModalityCodes.ProyectoGrado
                            ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                                ? i.ProjectPresentation.StateStage.Code
                            : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                                ? i.ProjectFinal.StateStage.Code
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.Code
                            : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                ? i.Proposal.StateStage.Code
                            : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PracticaAcademica
                            ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null ? i.AcademicPractice.StateStage.Code : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.CoTerminal
                            ? (i.CoTerminal != null && i.CoTerminal.StateStage != null ? i.CoTerminal.StateStage.Code : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SeminarioAct
                            ? (i.Seminar != null && i.Seminar.StateStage != null ? i.Seminar.StateStage.Code : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PublicacionArticulo
                            ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null ? i.ScientificArticle.StateStage.Code : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.GradoPromedio
                            ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null ? i.AcademicAverage.StateStage.Code : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SaberPro
                            ? (i.SaberPro != null && i.SaberPro.StateStage != null ? i.SaberPro.StateStage.Code : "")
                        : "",

                    EffectiveStateStageName =
                        i.Modality != null && i.Modality.Code == ModalityCodes.ProyectoGrado
                            ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                                ? i.ProjectPresentation.StateStage.Name
                            : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                                ? i.ProjectFinal.StateStage.Name
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.Name
                            : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                ? i.Proposal.StateStage.Name
                            : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PracticaAcademica
                            ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null ? i.AcademicPractice.StateStage.Name : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.CoTerminal
                            ? (i.CoTerminal != null && i.CoTerminal.StateStage != null ? i.CoTerminal.StateStage.Name : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SeminarioAct
                            ? (i.Seminar != null && i.Seminar.StateStage != null ? i.Seminar.StateStage.Name : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PublicacionArticulo
                            ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null ? i.ScientificArticle.StateStage.Name : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.GradoPromedio
                            ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null ? i.AcademicAverage.StateStage.Name : "")
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SaberPro
                            ? (i.SaberPro != null && i.SaberPro.StateStage != null ? i.SaberPro.StateStage.Name : "")
                        : "",

                    EffectiveIsFinalOverall =
                        i.Modality != null && i.Modality.Code == ModalityCodes.ProyectoGrado
                            ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                                ? i.ProjectPresentation.StateStage.IsFinalStateForModalityOverall
                            : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                                ? i.ProjectFinal.StateStage.IsFinalStateForModalityOverall
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.IsFinalStateForModalityOverall
                            : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                ? i.Proposal.StateStage.IsFinalStateForModalityOverall
                            : false)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PracticaAcademica
                            ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && i.AcademicPractice.StateStage.IsFinalStateForModalityOverall)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.CoTerminal
                            ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && i.CoTerminal.StateStage.IsFinalStateForModalityOverall)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SeminarioAct
                            ? (i.Seminar != null && i.Seminar.StateStage != null && i.Seminar.StateStage.IsFinalStateForModalityOverall)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PublicacionArticulo
                            ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && i.ScientificArticle.StateStage.IsFinalStateForModalityOverall)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.GradoPromedio
                            ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && i.AcademicAverage.StateStage.IsFinalStateForModalityOverall)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SaberPro
                            ? (i.SaberPro != null && i.SaberPro.StateStage != null && i.SaberPro.StateStage.IsFinalStateForModalityOverall)
                        : false,

                    EffectiveIsRejectionState =
                        i.Modality != null && i.Modality.Code == ModalityCodes.ProyectoGrado
                            ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && i.ProjectPresentation.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                                ? i.ProjectPresentation.StateStage.IsRejectionState
                            : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && i.ProjectFinal.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                                ? i.ProjectFinal.StateStage.IsRejectionState
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && i.PreliminaryProject.StateStage != null && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? i.PreliminaryProject.StateStage.IsRejectionState
                            : i.Proposal != null && i.Proposal.StatusRegister && i.Proposal.StateStage != null
                                ? i.Proposal.StateStage.IsRejectionState
                            : false)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PracticaAcademica
                            ? (i.AcademicPractice != null && i.AcademicPractice.StateStage != null && i.AcademicPractice.StateStage.IsRejectionState)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.CoTerminal
                            ? (i.CoTerminal != null && i.CoTerminal.StateStage != null && i.CoTerminal.StateStage.IsRejectionState)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SeminarioAct
                            ? (i.Seminar != null && i.Seminar.StateStage != null && i.Seminar.StateStage.IsRejectionState)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PublicacionArticulo
                            ? (i.ScientificArticle != null && i.ScientificArticle.StateStage != null && i.ScientificArticle.StateStage.IsRejectionState)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.GradoPromedio
                            ? (i.AcademicAverage != null && i.AcademicAverage.StateStage != null && i.AcademicAverage.StateStage.IsRejectionState)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SaberPro
                            ? (i.SaberPro != null && i.SaberPro.StateStage != null && i.SaberPro.StateStage.IsRejectionState)
                        : false,

                    ExtensionActivityAt =
                        i.Modality != null && i.Modality.Code == ModalityCodes.ProyectoGrado
                            ? (i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister && (i.StageModality == null || i.StageModality.StageOrder >= 4)
                                ? (i.ProjectPresentation.UpdatedAt ?? i.ProjectPresentation.ActualDateTime ?? i.ProjectPresentation.CreatedAt)
                            : i.ProjectFinal != null && i.ProjectFinal.StatusRegister && (i.StageModality == null || i.StageModality.StageOrder >= 3)
                                ? (i.ProjectFinal.UpdatedAt ?? i.ProjectFinal.FinalPhaseApprovalDate ?? i.ProjectFinal.ReportApprovalDate ?? i.ProjectFinal.CreatedAt)
                            : i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister && (i.StageModality == null || i.StageModality.StageOrder >= 2)
                                ? (i.PreliminaryProject.UpdatedAt ?? i.PreliminaryProject.ApprovalDate ?? i.PreliminaryProject.CreatedAt)
                            : i.Proposal != null && i.Proposal.StatusRegister
                                ? (i.Proposal.UpdatedAt ?? i.Proposal.CreatedAt)
                            : (i.UpdatedAt ?? i.CreatedAt))
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PracticaAcademica && i.AcademicPractice != null
                            ? (i.AcademicPractice.UpdatedAt ?? i.AcademicPractice.FinalApprovalDate ?? i.AcademicPractice.FinalReportApprovalDate ?? i.AcademicPractice.DevelopmentCompletionDate ?? i.AcademicPractice.PlanApprovalDate ?? i.AcademicPractice.AvalApprovalDate ?? i.AcademicPractice.CreatedAt)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.CoTerminal && i.CoTerminal != null
                            ? (i.CoTerminal.UpdatedAt ?? i.CoTerminal.CreatedAt)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SeminarioAct && i.Seminar != null
                            ? (i.Seminar.UpdatedAt ?? i.Seminar.CreatedAt)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.PublicacionArticulo && i.ScientificArticle != null
                            ? (i.ScientificArticle.UpdatedAt ?? i.ScientificArticle.CreatedAt)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.GradoPromedio && i.AcademicAverage != null
                            ? (i.AcademicAverage.UpdatedAt ?? i.AcademicAverage.CreatedAt)
                        : i.Modality != null && i.Modality.Code == ModalityCodes.SaberPro && i.SaberPro != null
                            ? (i.SaberPro.UpdatedAt ?? i.SaberPro.CreatedAt)
                        : (i.UpdatedAt ?? i.CreatedAt),

                    ProposalTitle = i.Proposal != null && i.Proposal.StatusRegister ? i.Proposal.Title : null,
                    PracticeTitle = i.AcademicPractice != null ? i.AcademicPractice.Title : null,
                    ArticleTitle = i.ScientificArticle != null ? i.ScientificArticle.ArticleTitle : null,
                    CoTerminalProgram = i.CoTerminal != null ? i.CoTerminal.PostgraduateProgramName : null,
                    CoTerminalUniversity = i.CoTerminal != null ? i.CoTerminal.UniversityName : null,
                    SeminarName = i.Seminar != null ? i.Seminar.SeminarName : null,

                    ProposalObs = i.Proposal != null && i.Proposal.StatusRegister ? i.Proposal.Observation : null,
                    PreliminaryObs = i.PreliminaryProject != null && i.PreliminaryProject.StatusRegister ? i.PreliminaryProject.Observations : null,
                    FinalObs = i.ProjectFinal != null && i.ProjectFinal.StatusRegister ? i.ProjectFinal.Observations : null,
                    PresentationObs = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister ? i.ProjectPresentation.Observations : null,
                    PracticeEvaluatorObs = i.AcademicPractice != null ? i.AcademicPractice.EvaluatorObservations : null,
                    PracticeObs = i.AcademicPractice != null ? i.AcademicPractice.Observations : null,
                    ArticleObs = i.ScientificArticle != null ? i.ScientificArticle.Observations : null,
                    CoTerminalObs = i.CoTerminal != null ? i.CoTerminal.Observations : null,
                    SeminarObs = i.Seminar != null ? i.Seminar.Observations : null,
                    AverageObs = i.AcademicAverage != null ? i.AcademicAverage.Observations : null,
                    SaberProObs = i.SaberPro != null ? i.SaberPro.Observations : null,

                    ScheduledDateTime = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister ? i.ProjectPresentation.ScheduledDateTime : null,
                    PresentationModality = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister ? i.ProjectPresentation.PresentationModality : null,
                    LocationOrLink = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister ? i.ProjectPresentation.LocationOrLink : null,
                    AttemptNumber = i.ProjectPresentation != null && i.ProjectPresentation.StatusRegister ? i.ProjectPresentation.AttemptNumber : 1
                })
                .Where(r => !r.EffectiveIsFinalOverall && !r.EffectiveIsRejectionState)
                .OrderBy(r => r.Id)
                .ToListAsync(cancellationToken);

            if (rawRows.Count == 0)
                return [];

            var activePeriodStarts = await _context.Set<AcademicPeriod>()
                .AsNoTracking()
                .Where(ap => ap.StatusRegister)
                .Select(ap => ap.StartDate)
                .ToListAsync(cancellationToken);

            var allIds = rawRows.Select(r => r.Id).ToList();
            var studentsById = new Dictionary<int, List<StudentRow>>();
            var juriesById = new Dictionary<int, List<string>>();
            var evalObsById = new Dictionary<int, string>();

            foreach (var chunk in allIds.Chunk(HydrationChunkSize))
            {
                var chunkIds = chunk.ToList();

                var studentRows = await _context.Set<UserInscriptionModality>()
                    .AsNoTracking()
                    .Where(uim => uim.StatusRegister
                        && uim.IsConfirmed
                        && uim.User != null
                        && uim.User.StatusRegister
                        && chunkIds.Contains(uim.IdInscriptionModality))
                    .Select(uim => new StudentRow
                    {
                        IdInscriptionModality = uim.IdInscriptionModality,
                        FullName = (uim.User!.FirstName + " " + uim.User.LastName).Trim(),
                        Email = uim.User.Email,
                        FacultyId = uim.User.AcademicProgram != null ? (int?)uim.User.AcademicProgram.IdFaculty : null
                    })
                    .ToListAsync(cancellationToken);

                foreach (var s in studentRows)
                {
                    if (!studentsById.TryGetValue(s.IdInscriptionModality, out var list))
                    {
                        list = [];
                        studentsById[s.IdInscriptionModality] = list;
                    }
                    list.Add(s);
                }

                var juryRows = await _context.Set<TeachingAssignment>()
                    .AsNoTracking()
                    .Where(ta => ta.StatusRegister
                        && ta.RevocationDate == null
                        && ta.IsConfirmed
                        && ta.Teacher != null
                        && ta.Teacher.StatusRegister
                        && ta.TypeTeachingAssignment != null
                        && chunkIds.Contains(ta.IdInscriptionModality)
                        && (ta.TypeTeachingAssignment.Code == "JURADO_EVALUADOR"
                            || ta.TypeTeachingAssignment.Code.ToUpper().Contains("JURADO")
                            || ta.TypeTeachingAssignment.Name.ToLower().Contains("jurado")))
                    .Select(ta => new
                    {
                        ta.IdInscriptionModality,
                        FullName = (ta.Teacher!.FirstName + " " + ta.Teacher.LastName).Trim()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var j in juryRows)
                {
                    if (string.IsNullOrWhiteSpace(j.FullName))
                        continue;

                    if (!juriesById.TryGetValue(j.IdInscriptionModality, out var list))
                    {
                        list = [];
                        juriesById[j.IdInscriptionModality] = list;
                    }
                    list.Add(j.FullName);
                }

                var evalRows = await _context.Set<Evaluation>()
                    .AsNoTracking()
                    .Where(e => e.StatusRegister
                        && chunkIds.Contains(e.EntityId)
                        && e.Observations != null
                        && e.Observations != "")
                    .OrderByDescending(e => e.CreatedAt)
                    .ThenByDescending(e => e.Id)
                    .Select(e => new { e.EntityId, e.Observations })
                    .ToListAsync(cancellationToken);

                foreach (var ev in evalRows)
                {
                    if (!evalObsById.ContainsKey(ev.EntityId) && !string.IsNullOrWhiteSpace(ev.Observations))
                    {
                        evalObsById[ev.EntityId] = ev.Observations.Trim();
                    }
                }
            }

            var candidates = new List<SlaAlertInscriptionCandidate>(rawRows.Count);
            foreach (var r in rawRows)
            {
                studentsById.TryGetValue(r.Id, out var students);
                juriesById.TryGetValue(r.Id, out var juries);

                var studentNames = students != null
                    ? string.Join(", ", students.Select(s => s.FullName).Where(x => !string.IsNullOrWhiteSpace(x)))
                    : string.Empty;

                var studentEmails = students != null
                    ? string.Join(", ", students
                        .Select(s => s.Email?.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase))
                    : string.Empty;

                var facultyId = students?
                    .Select(s => s.FacultyId)
                    .FirstOrDefault(f => f.HasValue && f.Value > 0);

                var juryNames = juries != null
                    ? string.Join(", ", juries.Distinct(StringComparer.OrdinalIgnoreCase))
                    : string.Empty;

                var lastActivityAt = r.UpdatedAt.HasValue && r.UpdatedAt.Value > r.ExtensionActivityAt
                    ? r.UpdatedAt.Value
                    : r.ExtensionActivityAt;

                var periodsElapsed = 0;
                if (r.PeriodStartDate.HasValue)
                {
                    var rawCount = activePeriodStarts.Count(start => start >= r.PeriodStartDate.Value && start <= nowUtc);
                    periodsElapsed = rawCount < 1 ? 1 : rawCount;
                }

                var workTitle = ResolveWorkTitle(r);
                var latestObs = ResolveLatestObservations(r);
                if (string.IsNullOrWhiteSpace(latestObs) && evalObsById.TryGetValue(r.Id, out var evalObs))
                {
                    latestObs = evalObs;
                }

                candidates.Add(new SlaAlertInscriptionCandidate
                {
                    InscriptionId = r.Id,
                    ModalityCode = r.ModalityCode,
                    ModalityName = r.ModalityName,
                    MaximumTermPeriods = r.MaximumTermPeriods,
                    StateInscriptionCode = r.StateInscriptionCode,
                    StateInscriptionName = r.StateInscriptionName,
                    AcademicPeriodCode = r.AcademicPeriodCode,
                    PeriodStartDate = r.PeriodStartDate,
                    PeriodsElapsed = periodsElapsed,
                    StageCode = r.StageCode,
                    StageName = r.StageName,
                    EffectiveStateStageCode = r.EffectiveStateStageCode,
                    EffectiveStateStageName = r.EffectiveStateStageName,
                    LastActivityAt = lastActivityAt,
                    WorkTitle = workTitle,
                    LatestObservations = latestObs,
                    StudentNames = studentNames,
                    StudentEmails = studentEmails,
                    FacultyId = facultyId,
                    ScheduledDateTime = r.ScheduledDateTime,
                    PresentationModality = r.PresentationModality ?? string.Empty,
                    LocationOrLink = r.LocationOrLink ?? string.Empty,
                    AttemptNumber = r.AttemptNumber > 0 ? r.AttemptNumber : 1,
                    JuryNames = juryNames
                });
            }

            return candidates;
        }

        public async Task<HashSet<string>> GetExistingDeduplicationKeysAsync(
            IEnumerable<string> candidateKeys,
            CancellationToken cancellationToken = default)
        {
            var distinctKeys = candidateKeys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var result = new HashSet<string>(StringComparer.Ordinal);
            if (distinctKeys.Count == 0)
                return result;

            foreach (var chunk in distinctKeys.Chunk(HydrationChunkSize))
            {
                var chunkList = chunk.ToList();
                var found = await _context.Set<EmailAlertDispatchLog>()
                    .AsNoTracking()
                    .Where(x => x.StatusRegister && chunkList.Contains(x.DeduplicationKey))
                    .Select(x => x.DeduplicationKey)
                    .ToListAsync(cancellationToken);

                foreach (var key in found)
                {
                    result.Add(key);
                }
            }

            return result;
        }

        public async Task<bool> TryRecordDispatchAsync(
            EmailAlertDispatchLog logEntry,
            CancellationToken cancellationToken = default)
        {
            var exists = await _context.Set<EmailAlertDispatchLog>()
                .AsNoTracking()
                .AnyAsync(x => x.DeduplicationKey == logEntry.DeduplicationKey && x.StatusRegister, cancellationToken);

            if (exists)
                return false;

            if (logEntry.CreatedAt == default)
                logEntry.CreatedAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(logEntry.OperationRegister))
                logEntry.OperationRegister = "SLA_ALERT_DISPATCH";

            logEntry.StatusRegister = true;

            _context.Set<EmailAlertDispatchLog>().Add(logEntry);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _context.Entry(logEntry).State = EntityState.Detached;
                _logger.LogWarning(
                    ex,
                    "Condición de carrera detectada al registrar EmailAlertDispatchLog para DeduplicationKey={DeduplicationKey}; se omite duplicado.",
                    logEntry.DeduplicationKey);
                return false;
            }
        }

        public async Task<SlaAlertSettingsSnapshot> GetSettingsSnapshotAsync(
            IReadOnlyCollection<string> slaEventNames,
            CancellationToken cancellationToken = default)
        {
            var config = await GetConfigurationAsync(cancellationToken);

            var eventList = slaEventNames.ToList();
            var emailConfigs = await _context.EmailNotificationConfigs
                .AsNoTracking()
                .Where(c => eventList.Contains(c.EventName))
                .OrderBy(c => c.Id)
                .ToListAsync(cancellationToken);

            var configIds = emailConfigs.Select(c => c.Id).ToList();
            var activeCcRules = configIds.Count > 0
                ? await _context.EmailRecipientRules
                    .AsNoTracking()
                    .Where(r => configIds.Contains(r.EmailNotificationConfigId)
                        && r.StatusRegister
                        && r.RecipientType == "CC")
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.Id)
                    .ToListAsync(cancellationToken)
                : [];

            var modalityLimits = await _context.Set<Modality>()
                .AsNoTracking()
                .Where(m => m.StatusRegister)
                .OrderBy(m => m.Id)
                .Select(m => new ModalityTermLimitDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    Name = m.Name,
                    MaximumTermPeriods = m.MaximumTermPeriods,
                    AllowsExtension = m.AllowsExtension
                })
                .ToListAsync(cancellationToken);

            return new SlaAlertSettingsSnapshot(config, emailConfigs, activeCcRules, modalityLimits);
        }

        public async Task SaveSettingsAsync(
            SlaAlertConfiguration updatedGlobal,
            IReadOnlyList<SlaAlertRulePersistenceInstruction> ruleInstructions,
            int? currentUserId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            var globalEntity = await _context.SlaAlertConfigurations
                .FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

            if (globalEntity == null)
            {
                globalEntity = new SlaAlertConfiguration
                {
                    Id = 1,
                    IsEnabled = updatedGlobal.IsEnabled,
                    ScheduleHourColombia = updatedGlobal.ScheduleHourColombia,
                    ScheduleMinuteColombia = updatedGlobal.ScheduleMinuteColombia,
                    WeekdaysOnly = updatedGlobal.WeekdaysOnly,
                    EvaluatorWarningBusinessDays = updatedGlobal.EvaluatorWarningBusinessDays,
                    EvaluatorOverdueBusinessDays = updatedGlobal.EvaluatorOverdueBusinessDays,
                    CommitteeOverdueBusinessDays = updatedGlobal.CommitteeOverdueBusinessDays,
                    StudentObservationsBusinessDays = updatedGlobal.StudentObservationsBusinessDays,
                    DefenseReminderHours = updatedGlobal.DefenseReminderHours,
                    WarnOnLastAllowedPeriod = updatedGlobal.WarnOnLastAllowedPeriod,
                    IdUserCreatedAt = currentUserId,
                    CreatedAt = nowUtc,
                    IdUserUpdatedAt = currentUserId,
                    UpdatedAt = nowUtc,
                    OperationRegister = "SLA_SETTINGS_CREATE",
                    StatusRegister = true
                };
                _context.SlaAlertConfigurations.Add(globalEntity);
            }
            else
            {
                globalEntity.IsEnabled = updatedGlobal.IsEnabled;
                globalEntity.ScheduleHourColombia = updatedGlobal.ScheduleHourColombia;
                globalEntity.ScheduleMinuteColombia = updatedGlobal.ScheduleMinuteColombia;
                globalEntity.WeekdaysOnly = updatedGlobal.WeekdaysOnly;
                globalEntity.EvaluatorWarningBusinessDays = updatedGlobal.EvaluatorWarningBusinessDays;
                globalEntity.EvaluatorOverdueBusinessDays = updatedGlobal.EvaluatorOverdueBusinessDays;
                globalEntity.CommitteeOverdueBusinessDays = updatedGlobal.CommitteeOverdueBusinessDays;
                globalEntity.StudentObservationsBusinessDays = updatedGlobal.StudentObservationsBusinessDays;
                globalEntity.DefenseReminderHours = updatedGlobal.DefenseReminderHours;
                globalEntity.WarnOnLastAllowedPeriod = updatedGlobal.WarnOnLastAllowedPeriod;
                globalEntity.StatusRegister = true;
                globalEntity.IdUserUpdatedAt = currentUserId;
                globalEntity.UpdatedAt = nowUtc;
                globalEntity.OperationRegister = "SLA_SETTINGS_UPDATE";
            }

            if (ruleInstructions.Count > 0)
            {
                var targetEvents = ruleInstructions.Select(r => r.EventName).Distinct().ToList();
                var existingConfigs = await _context.EmailNotificationConfigs
                    .Where(c => targetEvents.Contains(c.EventName))
                    .ToListAsync(cancellationToken);

                var configsByEvent = existingConfigs
                    .GroupBy(c => c.EventName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First(), StringComparer.OrdinalIgnoreCase);

                var createdNewConfigs = new List<(EmailNotificationConfig Config, SlaAlertRulePersistenceInstruction Instruction)>();

                foreach (var instruction in ruleInstructions)
                {
                    if (!configsByEvent.TryGetValue(instruction.EventName, out var configEntity))
                    {
                        configEntity = new EmailNotificationConfig
                        {
                            EventName = instruction.EventName,
                            SubjectTemplate = instruction.DefaultSubjectTemplate,
                            BodyTemplate = instruction.DefaultBodyTemplate,
                            IdUserCreatedAt = currentUserId,
                            CreatedAt = nowUtc,
                            IdUserUpdatedAt = currentUserId,
                            UpdatedAt = nowUtc,
                            OperationRegister = "SLA_SETTINGS_BOOTSTRAP",
                            StatusRegister = instruction.IsActive
                        };
                        _context.EmailNotificationConfigs.Add(configEntity);
                        configsByEvent[instruction.EventName] = configEntity;
                        createdNewConfigs.Add((configEntity, instruction));
                    }
                    else
                    {
                        configEntity.StatusRegister = instruction.IsActive;
                        configEntity.IdUserUpdatedAt = currentUserId;
                        configEntity.UpdatedAt = nowUtc;
                        configEntity.OperationRegister = "SLA_SETTINGS_UPDATE";
                    }
                }

                if (createdNewConfigs.Count > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    foreach (var (newConfig, instruction) in createdNewConfigs)
                    {
                        foreach (var toRule in instruction.DefaultToRules)
                        {
                            _context.EmailRecipientRules.Add(new EmailRecipientRule
                            {
                                EmailNotificationConfigId = newConfig.Id,
                                RecipientType = "TO",
                                RuleType = toRule.RuleType,
                                RuleValue = toRule.RuleValue,
                                Priority = toRule.Priority,
                                IdUserCreatedAt = currentUserId,
                                CreatedAt = nowUtc,
                                OperationRegister = "SLA_SETTINGS_BOOTSTRAP_TO",
                                StatusRegister = true
                            });
                        }
                    }
                }

                var configIds = configsByEvent.Values.Select(c => c.Id).ToList();

                // Cargar ÚNICAMENTE reglas CC (jamás se consultan con tracking ni se modifican filas TO / BCC)
                var existingCcRules = await _context.EmailRecipientRules
                    .Where(r => configIds.Contains(r.EmailNotificationConfigId) && r.RecipientType == "CC")
                    .OrderBy(r => r.Id)
                    .ToListAsync(cancellationToken);

                var nonCcMaxPriorities = await _context.EmailRecipientRules
                    .AsNoTracking()
                    .Where(r => configIds.Contains(r.EmailNotificationConfigId) && r.RecipientType != "CC")
                    .GroupBy(r => r.EmailNotificationConfigId)
                    .Select(g => new { ConfigId = g.Key, MaxPriority = g.Max(x => x.Priority) })
                    .ToDictionaryAsync(x => x.ConfigId, x => x.MaxPriority, cancellationToken);

                foreach (var instruction in ruleInstructions)
                {
                    var configEntity = configsByEvent[instruction.EventName];
                    var ccRowsForConfig = existingCcRules
                        .Where(r => r.EmailNotificationConfigId == configEntity.Id)
                        .ToList();

                    var basePriority = nonCcMaxPriorities.TryGetValue(configEntity.Id, out var maxP)
                        ? Math.Max(1, maxP)
                        : Math.Max(1, instruction.DefaultToRules.Count);

                    var matchedRuleIds = new HashSet<int>();

                    for (var idx = 0; idx < instruction.SelectedCcTargets.Count; idx++)
                    {
                        var target = instruction.SelectedCcTargets[idx];
                        var desiredPriority = basePriority + idx + 1;

                        var existingMatch = ccRowsForConfig.FirstOrDefault(r =>
                            !matchedRuleIds.Contains(r.Id)
                            && string.Equals(r.RuleType, target.RuleType, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(r.RuleValue, target.RuleValue, StringComparison.OrdinalIgnoreCase));

                        if (existingMatch != null)
                        {
                            matchedRuleIds.Add(existingMatch.Id);
                            existingMatch.StatusRegister = true;
                            existingMatch.Priority = desiredPriority;
                            existingMatch.IdUserUpdatedAt = currentUserId;
                            existingMatch.UpdatedAt = nowUtc;
                            existingMatch.OperationRegister = "SLA_SETTINGS_ENABLE_CC";
                        }
                        else
                        {
                            _context.EmailRecipientRules.Add(new EmailRecipientRule
                            {
                                EmailNotificationConfigId = configEntity.Id,
                                RecipientType = "CC",
                                RuleType = target.RuleType,
                                RuleValue = target.RuleValue,
                                Priority = desiredPriority,
                                IdUserCreatedAt = currentUserId,
                                CreatedAt = nowUtc,
                                OperationRegister = "SLA_SETTINGS_ADD_CC",
                                StatusRegister = true
                            });
                        }
                    }

                    foreach (var unmatched in ccRowsForConfig.Where(r => !matchedRuleIds.Contains(r.Id) && r.StatusRegister))
                    {
                        unmatched.StatusRegister = false;
                        unmatched.IdUserUpdatedAt = currentUserId;
                        unmatched.UpdatedAt = nowUtc;
                        unmatched.OperationRegister = "SLA_SETTINGS_DISABLE_CC";
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SlaAlertDispatchLogsPageDto> GetDispatchLogsAsync(
            int page,
            int pageSize,
            string? alertCode,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = Math.Clamp(pageSize < 1 ? 15 : pageSize, 1, 100);

            var query = _context.EmailAlertDispatchLogs
                .AsNoTracking()
                .Where(x => x.StatusRegister);

            if (!string.IsNullOrWhiteSpace(alertCode))
            {
                var trimmedCode = alertCode.Trim();
                query = query.Where(x => x.AlertCode == trimmedCode);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                var hasId = int.TryParse(term, out var idSearch);

                query = query.Where(x =>
                    (hasId && x.IdInscriptionModality == idSearch)
                    || (x.RecipientsSummary != null && x.RecipientsSummary.ToLower().Contains(term))
                    || x.AlertCode.ToLower().Contains(term)
                    || x.EventName.ToLower().Contains(term)
                    || x.StageStateSnapshot.ToLower().Contains(term)
                    || (x.InscriptionModality != null
                        && x.InscriptionModality.Modality != null
                        && (x.InscriptionModality.Modality.Name.ToLower().Contains(term)
                            || x.InscriptionModality.Modality.Code.ToLower().Contains(term)))
                    || (x.InscriptionModality != null
                        && x.InscriptionModality.UserInscriptionModalities.Any(uim =>
                            uim.StatusRegister
                            && uim.IsConfirmed
                            && uim.User != null
                            && ((uim.User.FirstName + " " + uim.User.LastName).ToLower().Contains(term)
                                || uim.User.Email.ToLower().Contains(term)
                                || uim.User.Identification.ToLower().Contains(term)))));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rawItems = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(x => new
                {
                    x.Id,
                    x.CreatedAt,
                    x.IdInscriptionModality,
                    ModalityName = x.InscriptionModality != null && x.InscriptionModality.Modality != null
                        ? x.InscriptionModality.Modality.Name
                        : "",
                    x.AlertCode,
                    x.EventName,
                    x.StageStateSnapshot,
                    x.BusinessDaysElapsed,
                    x.RecipientsSummary
                })
                .ToListAsync(cancellationToken);

            var inscriptionIds = rawItems.Select(x => x.IdInscriptionModality).Distinct().ToList();
            var studentsByInscription = new Dictionary<int, string>();
            var modalityByInscription = new Dictionary<int, string>();

            if (inscriptionIds.Count > 0)
            {
                var studentRows = await _context.Set<UserInscriptionModality>()
                    .AsNoTracking()
                    .Where(uim => uim.StatusRegister
                        && uim.IsConfirmed
                        && uim.User != null
                        && inscriptionIds.Contains(uim.IdInscriptionModality))
                    .Select(uim => new
                    {
                        uim.IdInscriptionModality,
                        FullName = (uim.User!.FirstName + " " + uim.User.LastName).Trim()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var grp in studentRows.GroupBy(s => s.IdInscriptionModality))
                {
                    studentsByInscription[grp.Key] = string.Join(
                        ", ",
                        grp.Select(x => x.FullName).Where(n => !string.IsNullOrWhiteSpace(n)));
                }

                if (rawItems.Any(x => string.IsNullOrWhiteSpace(x.ModalityName)))
                {
                    var modRows = await _context.Set<InscriptionModality>()
                        .AsNoTracking()
                        .Where(i => inscriptionIds.Contains(i.Id) && i.Modality != null)
                        .Select(i => new { i.Id, ModalityName = i.Modality!.Name })
                        .ToListAsync(cancellationToken);

                    foreach (var m in modRows)
                    {
                        modalityByInscription[m.Id] = m.ModalityName;
                    }
                }
            }

            var items = rawItems.Select(x => new SlaAlertDispatchLogItemDto
            {
                Id = x.Id,
                CreatedAtColombia = DashboardTimeHelper.FormatDateTime(x.CreatedAt),
                IdInscriptionModality = x.IdInscriptionModality,
                ModalityName = !string.IsNullOrWhiteSpace(x.ModalityName)
                    ? x.ModalityName
                    : (modalityByInscription.TryGetValue(x.IdInscriptionModality, out var fallbackMod) ? fallbackMod : string.Empty),
                StudentsSummary = studentsByInscription.TryGetValue(x.IdInscriptionModality, out var students)
                    ? students
                    : string.Empty,
                AlertCode = x.AlertCode,
                EventName = x.EventName,
                StageStateSnapshot = x.StageStateSnapshot,
                BusinessDaysElapsed = x.BusinessDaysElapsed,
                RecipientsSummary = x.RecipientsSummary
            }).ToList();

            return new SlaAlertDispatchLogsPageDto
            {
                TotalCount = totalCount,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Items = items
            };
        }

        private static string ResolveWorkTitle(RawCandidateProjection r)
        {
            var rawTitle = r.ModalityCode switch
            {
                ModalityCodes.ProyectoGrado => r.ProposalTitle,
                ModalityCodes.PracticaAcademica => r.PracticeTitle,
                ModalityCodes.PublicacionArticulo => r.ArticleTitle,
                ModalityCodes.CoTerminal => FirstNonEmpty(r.CoTerminalProgram, r.CoTerminalUniversity),
                ModalityCodes.SeminarioAct => r.SeminarName,
                _ => null
            };

            return FirstNonEmpty(rawTitle, r.ModalityName, "Modalidad de grado");
        }

        private static string ResolveLatestObservations(RawCandidateProjection r)
        {
            var stageOrder = r.StageOrder ?? 1;

            var extObs = r.ModalityCode switch
            {
                ModalityCodes.ProyectoGrado => stageOrder >= 4
                    ? FirstNonEmpty(r.PresentationObs, r.FinalObs, r.PreliminaryObs, r.ProposalObs)
                    : stageOrder == 3
                        ? FirstNonEmpty(r.FinalObs, r.PreliminaryObs, r.ProposalObs)
                        : stageOrder == 2
                            ? FirstNonEmpty(r.PreliminaryObs, r.ProposalObs)
                            : r.ProposalObs,
                ModalityCodes.PracticaAcademica => FirstNonEmpty(r.PracticeEvaluatorObs, r.PracticeObs),
                ModalityCodes.PublicacionArticulo => r.ArticleObs,
                ModalityCodes.CoTerminal => r.CoTerminalObs,
                ModalityCodes.SeminarioAct => r.SeminarObs,
                ModalityCodes.GradoPromedio => r.AverageObs,
                ModalityCodes.SaberPro => r.SaberProObs,
                _ => null
            };

            return FirstNonEmpty(extObs, r.InscriptionObservations);
        }

        private static string FirstNonEmpty(params string?[] candidates)
        {
            foreach (var c in candidates)
            {
                if (!string.IsNullOrWhiteSpace(c))
                    return c.Trim();
            }
            return string.Empty;
        }

        private sealed class RawCandidateProjection
        {
            public int Id { get; set; }
            public string ModalityCode { get; set; } = string.Empty;
            public string ModalityName { get; set; } = string.Empty;
            public int? MaximumTermPeriods { get; set; }
            public string StateInscriptionCode { get; set; } = string.Empty;
            public string StateInscriptionName { get; set; } = string.Empty;
            public string AcademicPeriodCode { get; set; } = string.Empty;
            public DateTimeOffset? PeriodStartDate { get; set; }
            public int? StageOrder { get; set; }
            public string StageCode { get; set; } = string.Empty;
            public string StageName { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
            public string? InscriptionObservations { get; set; }
            public string EffectiveStateStageCode { get; set; } = string.Empty;
            public string EffectiveStateStageName { get; set; } = string.Empty;
            public bool EffectiveIsFinalOverall { get; set; }
            public bool EffectiveIsRejectionState { get; set; }
            public DateTimeOffset ExtensionActivityAt { get; set; }
            public string? ProposalTitle { get; set; }
            public string? PracticeTitle { get; set; }
            public string? ArticleTitle { get; set; }
            public string? CoTerminalProgram { get; set; }
            public string? CoTerminalUniversity { get; set; }
            public string? SeminarName { get; set; }
            public string? ProposalObs { get; set; }
            public string? PreliminaryObs { get; set; }
            public string? FinalObs { get; set; }
            public string? PresentationObs { get; set; }
            public string? PracticeEvaluatorObs { get; set; }
            public string? PracticeObs { get; set; }
            public string? ArticleObs { get; set; }
            public string? CoTerminalObs { get; set; }
            public string? SeminarObs { get; set; }
            public string? AverageObs { get; set; }
            public string? SaberProObs { get; set; }
            public DateTimeOffset? ScheduledDateTime { get; set; }
            public string? PresentationModality { get; set; }
            public string? LocationOrLink { get; set; }
            public int AttemptNumber { get; set; } = 1;
        }

        private sealed class StudentRow
        {
            public int IdInscriptionModality { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int? FacultyId { get; set; }
        }
    }
}
