using System.Globalization;
using Application.Common.Helpers;
using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Motor institucional de alertas automáticas de vencimientos, SLAs normativos (Acuerdo 015 de 2021)
    /// y recordatorios operativos de modalidades de grado con deduplicación persistente.
    /// </summary>
    public class SlaDeadlineAlertService : ISlaDeadlineAlertService
    {
        private static readonly SemaphoreSlim ExecutionGate = new(1, 1);

        public const string AlertEvaluatorApproaching = "SLA_EVALUATOR_APPROACHING";
        public const string AlertEvaluatorOverdue = "SLA_EVALUATOR_OVERDUE";
        public const string AlertCommitteeOverdue = "SLA_COMMITTEE_REVIEW_OVERDUE";
        public const string AlertDefenseReminder = "SLA_DEFENSE_REMINDER";
        public const string AlertStudentObservations = "SLA_STUDENT_OBSERVATIONS_REMINDER";
        public const string AlertTermLimitWarning = "SLA_TERM_LIMIT_WARNING";

        private static readonly HashSet<string> EvaluatorSlaStates = new(StringComparer.OrdinalIgnoreCase)
        {
            StateStageCodes.ApEnEvaluacion,
            StateStageCodes.PfinfRadicadoEnEvaluacion,
            StateStageCodes.PaInformeFinalEnRevision
        };

        private static readonly HashSet<string> CommitteeReviewStates = new(StringComparer.OrdinalIgnoreCase)
        {
            StateStageCodes.PropRadicada,
            StateStageCodes.ApRadicadoPendAsigEval,
            StateStageCodes.SustPendienteProgramacion,
            StateStageCodes.PaInscripcionEnRevision,
            "PA_DESARROLLO_EN_REVISION",
            "ART_INS_RADICADO",
            "ART_PUB_RADICADO",
            "SP_RADICADO",
            "GP_RADICADO",
            "CT_RADICADO",
            "SEM_RADICADO"
        };

        private readonly ISlaAlertRepository _slaAlertRepository;
        private readonly IEmailNotificationEventService _emailEventService;
        private readonly IConfiguration? _configuration;
        private readonly ILogger<SlaDeadlineAlertService> _logger;
        private readonly TimeProvider _timeProvider;

        public SlaDeadlineAlertService(
            ISlaAlertRepository slaAlertRepository,
            IEmailNotificationEventService emailEventService,
            ILogger<SlaDeadlineAlertService> logger,
            IConfiguration? configuration = null,
            TimeProvider? timeProvider = null)
        {
            _slaAlertRepository = slaAlertRepository;
            _emailEventService = emailEventService;
            _logger = logger;
            _configuration = configuration;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        public async Task<SlaAlertExecutionSummary> ProcessDueAlertsAsync(CancellationToken cancellationToken = default)
        {
            await ExecutionGate.WaitAsync(cancellationToken);
            try
            {
                var dbConfig = await _slaAlertRepository.GetConfigurationAsync(cancellationToken);

                var isEnabled = dbConfig?.IsEnabled ?? GetBoolConfig("SlaAlerts:Enabled", defaultValue: true);
                if (!isEnabled)
                {
                    _logger.LogInformation("Motor de alertas SLA deshabilitado por configuración.");
                    return new SlaAlertExecutionSummary(0, 0, 0, 0, []);
                }

                var evaluatorWarningDays = dbConfig?.EvaluatorWarningBusinessDays ?? GetIntConfig("SlaAlerts:EvaluatorWarningBusinessDays", 20);
                var evaluatorOverdueDays = dbConfig?.EvaluatorOverdueBusinessDays ?? GetIntConfig("SlaAlerts:EvaluatorOverdueBusinessDays", 25);
                var committeeOverdueDays = dbConfig?.CommitteeOverdueBusinessDays ?? GetIntConfig("SlaAlerts:CommitteeOverdueBusinessDays", 10);
                var studentObservationsDays = dbConfig?.StudentObservationsBusinessDays ?? GetIntConfig("SlaAlerts:StudentObservationsBusinessDays", 15);
                var defenseReminderHours = dbConfig?.DefenseReminderHours ?? GetIntConfig("SlaAlerts:DefenseReminderHours", 48);
                var warnOnLastAllowedPeriod = dbConfig?.WarnOnLastAllowedPeriod ?? GetBoolConfig("SlaAlerts:WarnOnLastAllowedPeriod", defaultValue: true);

                var nowUtc = _timeProvider.GetUtcNow();
                var candidates = await _slaAlertRepository.GetActiveCandidatesAsync(nowUtc, cancellationToken);

                if (candidates.Count == 0)
                {
                    var emptySummary = new SlaAlertExecutionSummary(0, 0, 0, 0, []);
                    if (dbConfig != null)
                    {
                        await _slaAlertRepository.SaveConfigurationRunMetricsAsync(nowUtc, emptySummary, cancellationToken);
                    }
                    return emptySummary;
                }

                var pendingAlerts = new List<PendingAlertCandidate>();

                foreach (var candidate in candidates)
                {
                    EvaluateStageAndScheduleAlerts(
                        candidate,
                        nowUtc,
                        evaluatorWarningDays,
                        evaluatorOverdueDays,
                        committeeOverdueDays,
                        studentObservationsDays,
                        defenseReminderHours,
                        pendingAlerts);

                    EvaluateTermLimitAlert(candidate, warnOnLastAllowedPeriod, pendingAlerts);
                }

                if (pendingAlerts.Count == 0)
                {
                    var noPendingSummary = new SlaAlertExecutionSummary(candidates.Count, 0, 0, 0, []);
                    if (dbConfig != null)
                    {
                        await _slaAlertRepository.SaveConfigurationRunMetricsAsync(nowUtc, noPendingSummary, cancellationToken);
                    }
                    return noPendingSummary;
                }

                var existingKeys = await _slaAlertRepository.GetExistingDeduplicationKeysAsync(
                    pendingAlerts.Select(a => a.DeduplicationKey),
                    cancellationToken);

                var activeConfigCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                var dispatchedKeys = new List<string>();
                var triggered = 0;
                var skipped = 0;
                var failed = 0;

                foreach (var alert in pendingAlerts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (existingKeys.Contains(alert.DeduplicationKey))
                    {
                        skipped++;
                        continue;
                    }

                    if (!activeConfigCache.TryGetValue(alert.EventName, out var hasActiveConfig))
                    {
                        hasActiveConfig = await _emailEventService.HasActiveConfigurationAsync(alert.EventName);
                        activeConfigCache[alert.EventName] = hasActiveConfig;
                    }

                    if (!hasActiveConfig)
                    {
                        _logger.LogDebug(
                            "Sin configuración activa para el evento SLA {EventName}; se omite inscripción {InscriptionId}.",
                            alert.EventName,
                            alert.InscriptionId);
                        continue;
                    }

                    var logEntry = new EmailAlertDispatchLog
                    {
                        IdInscriptionModality = alert.InscriptionId,
                        AlertCode = alert.AlertCode,
                        EventName = alert.EventName,
                        StageStateSnapshot = alert.StageStateSnapshot,
                        DeduplicationKey = alert.DeduplicationKey,
                        RecipientsSummary = null,
                        BusinessDaysElapsed = alert.BusinessDaysElapsed,
                        CreatedAt = nowUtc,
                        OperationRegister = "SLA_CLAIM_PENDING",
                        StatusRegister = true
                    };

                    var claimed = await _slaAlertRepository.TryClaimDispatchAsync(logEntry, cancellationToken);
                    if (!claimed)
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        var dispatch = await _emailEventService.ProcessEventWithSummaryAsync(alert.EventName, alert.EventData);

                        if (dispatch.EmailsSent == 0)
                        {
                            await _slaAlertRepository.ReleaseDispatchClaimAsync(logEntry.Id, CancellationToken.None);
                            continue;
                        }

                        await _slaAlertRepository.ConfirmDispatchClaimAsync(
                            logEntry.Id,
                            string.Join(", ", dispatch.Recipients),
                            cancellationToken);

                        existingKeys.Add(alert.DeduplicationKey);
                        triggered++;
                        dispatchedKeys.Add(alert.DeduplicationKey);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        await _slaAlertRepository.ReleaseDispatchClaimAsync(logEntry.Id, CancellationToken.None);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await _slaAlertRepository.ReleaseDispatchClaimAsync(logEntry.Id, CancellationToken.None);
                        failed++;
                        _logger.LogError(
                            ex,
                            "Error al procesar alerta SLA {AlertCode} para inscripción {InscriptionId} (DedupKey={DeduplicationKey}).",
                            alert.AlertCode,
                            alert.InscriptionId,
                            alert.DeduplicationKey);
                    }
                }

                _logger.LogInformation(
                    "Ejecución SLA completada: Evaluadas={Evaluated}, Enviadas={Triggered}, OmitidasYaEnviadas={Skipped}, Fallidas={Failed}.",
                    candidates.Count,
                    triggered,
                    skipped,
                    failed);

                var summary = new SlaAlertExecutionSummary(
                    EvaluatedInscriptions: candidates.Count,
                    AlertsTriggered: triggered,
                    AlertsSkippedAlreadySent: skipped,
                    AlertsFailed: failed,
                    DispatchedKeys: dispatchedKeys);

                if (dbConfig != null)
                {
                    await _slaAlertRepository.SaveConfigurationRunMetricsAsync(nowUtc, summary, cancellationToken);
                }

                return summary;
            }
            finally
            {
                ExecutionGate.Release();
            }
        }

        private static void EvaluateStageAndScheduleAlerts(
            SlaAlertInscriptionCandidate c,
            DateTimeOffset nowUtc,
            int evaluatorWarningDays,
            int evaluatorOverdueDays,
            int committeeOverdueDays,
            int studentObservationsDays,
            int defenseReminderHours,
            List<PendingAlertCandidate> output)
        {
            var effectiveStateCode = (c.EffectiveStateStageCode ?? string.Empty).Trim().ToUpperInvariant();
            var stateInscriptionCode = (c.StateInscriptionCode ?? string.Empty).Trim().ToUpperInvariant();
            var modalityCode = (c.ModalityCode ?? string.Empty).Trim().ToUpperInvariant();
            var stageCode = !string.IsNullOrWhiteSpace(c.StageCode) ? c.StageCode.Trim().ToUpperInvariant() : "SIN_FASE";
            var activitySince = c.LastActivityAt;
            var bizDays = DashboardTimeHelper.CountBusinessDaysElapsed(activitySince, nowUtc);
            var activityDateIso = DashboardTimeHelper.FormatDate(activitySince);
            var activityDateDisplay = NotificationFormattingHelper.ToColombiaDate(activitySince);

            // 1. SLA de evaluación docente / jurados (Acuerdo 015 Arts. 15 y 24 — máx. 25 días hábiles)
            if (EvaluatorSlaStates.Contains(effectiveStateCode))
            {
                if (bizDays >= evaluatorOverdueDays || bizDays >= evaluatorWarningDays)
                {
                    var isOverdue = bizDays >= evaluatorOverdueDays;
                    var alertCode = isOverdue ? AlertEvaluatorOverdue : AlertEvaluatorApproaching;
                    var snapshot = $"{stageCode}:{effectiveStateCode}:{activityDateIso}";
                    var remaining = isOverdue ? 0 : Math.Max(1, evaluatorOverdueDays - bizDays);
                    var accent = isOverdue ? "#DC2626" : "#D97706";
                    var normativeReference = modalityCode == ModalityCodes.PracticaAcademica
                        ? $"Acuerdo No. 015 de 2021, Artículo 24 (máximo {evaluatorOverdueDays} días hábiles)"
                        : $"Acuerdo No. 015 de 2021, Artículo 15 (máximo {evaluatorOverdueDays} días hábiles)";

                    var eventData = CreateCommonEventData(c, accent);
                    eventData["ActivitySinceDate"] = activityDateDisplay;
                    eventData["BusinessDaysElapsed"] = bizDays;
                    eventData["MaxBusinessDays"] = evaluatorOverdueDays;
                    eventData["RemainingBusinessDays"] = remaining;
                    eventData["NormativeReference"] = normativeReference;

                    output.Add(PendingAlertCandidate.Create(
                        c.InscriptionId,
                        alertCode,
                        snapshot,
                        bizDays,
                        $"STAGE_EVALUATOR_RESPONSIBLE ({modalityCode})",
                        eventData));
                }

                return;
            }

            // 2. Revisión / programación pendiente del Comité o Coordinación
            var isPendingInitialApproval = stateInscriptionCode == StateInscriptionCodes.Pendiente;
            var isPendingCommitteeStage = CommitteeReviewStates.Contains(effectiveStateCode);

            if (isPendingInitialApproval || isPendingCommitteeStage)
            {
                if (bizDays >= committeeOverdueDays)
                {
                    var stateSnapshotToken = !string.IsNullOrWhiteSpace(effectiveStateCode)
                        ? effectiveStateCode
                        : stateInscriptionCode;
                    var stageSnapshotToken = !string.IsNullOrWhiteSpace(c.StageCode)
                        ? stageCode
                        : "INSCRIPCION";
                    var snapshot = $"{stageSnapshotToken}:{stateSnapshotToken}:{activityDateIso}";

                    var eventData = CreateCommonEventData(c, "#D97706");
                    eventData["ActivitySinceDate"] = activityDateDisplay;
                    eventData["BusinessDaysElapsed"] = bizDays;
                    eventData["PendingActionDescription"] = BuildPendingCommitteeActionDescription(
                        isPendingInitialApproval,
                        effectiveStateCode);

                    output.Add(PendingAlertCandidate.Create(
                        c.InscriptionId,
                        AlertCommitteeOverdue,
                        snapshot,
                        bizDays,
                        $"COMMITTEE_STAGE_RESPONSIBLE ({modalityCode}:{stageSnapshotToken})",
                        eventData));
                }

                return;
            }

            // 3. Recordatorio previo de sustentación pública programada (ventana de N horas)
            if (modalityCode == ModalityCodes.ProyectoGrado
                && effectiveStateCode == StateStageCodes.SustProgramada
                && c.ScheduledDateTime.HasValue)
            {
                var scheduledUtc = c.ScheduledDateTime.Value;
                var upperBoundUtc = nowUtc + TimeSpan.FromHours(defenseReminderHours);

                if (scheduledUtc > nowUtc && scheduledUtc <= upperBoundUtc)
                {
                    var snapshot = $"SUST_PROGRAMADA:{c.AttemptNumber}:{scheduledUtc.UtcDateTime:yyyyMMddHHmm}";
                    var eventData = CreateCommonEventData(c, "#006633");
                    eventData["ScheduledDateTime"] = NotificationFormattingHelper.ToColombiaDateTime(scheduledUtc);
                    eventData["PresentationModality"] = FormatDefenseModalityLabel(c.PresentationModality);
                    eventData["LocationOrLink"] = !string.IsNullOrWhiteSpace(c.LocationOrLink)
                        ? c.LocationOrLink
                        : "Por confirmar en SIGEPRO";
                    eventData["AttemptNumber"] = c.AttemptNumber;
                    eventData["JuryNames"] = !string.IsNullOrWhiteSpace(c.JuryNames)
                        ? c.JuryNames
                        : "Por confirmar";

                    output.Add(PendingAlertCandidate.Create(
                        c.InscriptionId,
                        AlertDefenseReminder,
                        snapshot,
                        0,
                        FirstNonEmpty(c.StudentEmails, "SUST_PROGRAMADA"),
                        eventData));
                }

                return;
            }

            // 4. Recordatorio al estudiante con observaciones pendientes de subsanar
            var isWithObservations = effectiveStateCode.Contains("OBSERVACIONES", StringComparison.OrdinalIgnoreCase)
                || effectiveStateCode == StateStageCodes.PropNoPertinente
                || effectiveStateCode == StateStageCodes.SustNoAprobadaReintento;

            if (isWithObservations && bizDays >= studentObservationsDays)
            {
                var snapshot = $"{stageCode}:{effectiveStateCode}:{activityDateIso}";
                var eventData = CreateCommonEventData(c, "#D97706");
                eventData["ActivitySinceDate"] = activityDateDisplay;
                eventData["BusinessDaysElapsed"] = bizDays;
                eventData["ObservationsBlock"] = NotificationFormattingHelper.BuildObservationsBlockHtml(
                    c.LatestObservations,
                    "warning");

                output.Add(PendingAlertCandidate.Create(
                    c.InscriptionId,
                    AlertStudentObservations,
                    snapshot,
                    bizDays,
                    c.StudentEmails,
                    eventData));
            }
        }

        private static void EvaluateTermLimitAlert(
            SlaAlertInscriptionCandidate c,
            bool warnOnLastAllowedPeriod,
            List<PendingAlertCandidate> output)
        {
            if (!c.MaximumTermPeriods.HasValue || c.MaximumTermPeriods.Value <= 0 || c.PeriodsElapsed <= 0)
                return;

            var maxTerm = c.MaximumTermPeriods.Value;
            var elapsed = c.PeriodsElapsed;

            if (elapsed < maxTerm)
                return;

            var isExceeded = elapsed > maxTerm;
            if (!isExceeded && !warnOnLastAllowedPeriod)
                return;
            var riskLevel = isExceeded ? "EXCEEDED" : "WARNING";
            var riskLabel = isExceeded
                ? "Plazo reglamentario excedido"
                : "Último periodo académico permitido";
            var accent = isExceeded ? "#DC2626" : "#D97706";
            var snapshot = $"{riskLevel}:{elapsed}_OF_{maxTerm}";

            var normativeGuidance = isExceeded
                ? "Conforme al Acuerdo No. 015 de 2021 (Artículos 7 y 8), la inscripción ha superado el número máximo de periodos académicos ordinarios permitidos para la modalidad. El caso requiere revisión prioritaria por parte del Comité Curricular y de Trabajo de Grado y, de corresponder, remisión o concepto ante el Consejo de Facultad."
                : "Conforme al Acuerdo No. 015 de 2021 (Artículos 7 y 8), usted cursa actualmente el último periodo académico ordinario permitido para culminar su modalidad de grado. Si requiere prórroga o aplazamiento justificado, debe radicar la solicitud formal ante el Comité Curricular y de Trabajo de Grado con al menos treinta (30) días calendario de antelación a la finalización oficial del periodo académico.";

            var eventData = CreateCommonEventData(c, accent);
            eventData["StartAcademicPeriod"] = FirstNonEmpty(c.AcademicPeriodCode, "N/D");
            eventData["PeriodsElapsed"] = elapsed;
            eventData["MaxTermPeriods"] = maxTerm;
            eventData["RiskStatusLabel"] = riskLabel;
            eventData["NormativeGuidance"] = normativeGuidance;

            output.Add(PendingAlertCandidate.Create(
                c.InscriptionId,
                AlertTermLimitWarning,
                snapshot,
                0,
                FirstNonEmpty(c.StudentEmails, "SLA_TERM_LIMIT_WARNING"),
                eventData));
        }

        private static Dictionary<string, object> CreateCommonEventData(
            SlaAlertInscriptionCandidate c,
            string topAccentColor)
        {
            var currentStageName = FirstNonEmpty(c.StageName, "Inscripción inicial");
            var currentStateName = FirstNonEmpty(c.EffectiveStateStageName, c.StateInscriptionName, "En trámite");
            var studentNames = FirstNonEmpty(c.StudentNames, "Estudiante registrado");
            var workTitle = FirstNonEmpty(c.WorkTitle, c.ModalityName, "Modalidad de grado");
            var academicPeriod = FirstNonEmpty(c.AcademicPeriodCode, "Vigente");

            var data = new Dictionary<string, object>
            {
                ["InscriptionModalityId"] = c.InscriptionId,
                ["InscriptionId"] = c.InscriptionId,
                ["TopAccentColor"] = topAccentColor,
                ["ModalityName"] = FirstNonEmpty(c.ModalityName, c.ModalityCode),
                ["CurrentStageName"] = currentStageName,
                ["CurrentStateName"] = currentStateName,
                ["WorkTitle"] = workTitle,
                ["StudentNames"] = studentNames,
                ["StudentEmails"] = c.StudentEmails ?? string.Empty,
                ["AcademicPeriod"] = academicPeriod
            };

            if (c.FacultyId is > 0)
            {
                data["FacultyId"] = c.FacultyId.Value;
            }

            return data;
        }

        private static string BuildPendingCommitteeActionDescription(
            bool isPendingInitialApproval,
            string effectiveStateCode)
        {
            if (isPendingInitialApproval)
                return "Revisión de aval inicial de inscripción a modalidad de grado";

            return effectiveStateCode switch
            {
                StateStageCodes.PropRadicada => "Revisión de pertinencia y dictamen del Comité sobre propuesta de grado",
                StateStageCodes.ApRadicadoPendAsigEval => "Asignación de jurados evaluadores de anteproyecto",
                StateStageCodes.SustPendienteProgramacion => "Programación de fecha, modalidad y lugar o enlace de sustentación pública",
                StateStageCodes.PaInscripcionEnRevision => "Revisión y aval de documentación de inscripción de Práctica Académica",
                "PA_DESARROLLO_EN_REVISION" => "Revisión de soportes de seguimiento y desarrollo de Práctica Académica",
                "ART_INS_RADICADO" => "Revisión del Comité sobre solicitud inicial de Publicación de Artículo",
                "ART_PUB_RADICADO" => "Revisión y dictamen del Comité sobre artículo publicado radicado",
                "SP_RADICADO" => "Revisión y dictamen del Comité sobre documentación de Saber Pro",
                "GP_RADICADO" => "Revisión y verificación de requisitos de Grado por Promedio",
                "CT_RADICADO" => "Revisión y dictamen del Comité sobre documentación de modalidad Co-Terminal",
                "SEM_RADICADO" => "Revisión y dictamen del Comité sobre documentación de Seminario de Actualización",
                _ => "Revisión y dictamen del Comité sobre documentación radicada"
            };
        }

        private static string FormatDefenseModalityLabel(string? rawModality)
        {
            if (string.IsNullOrWhiteSpace(rawModality))
                return "Por definir";

            var normalized = rawModality.Trim().ToUpperInvariant();
            return normalized switch
            {
                "PRESENCIAL" => "Presencial",
                "VIRTUAL" => "Virtual",
                "MIXTA" => "Mixta",
                _ => rawModality.Trim()
            };
        }

        private bool GetBoolConfig(string key, bool defaultValue)
        {
            var raw = _configuration?[key];
            return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
        }

        private int GetIntConfig(string key, int defaultValue)
        {
            var raw = _configuration?[key];
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : defaultValue;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return string.Empty;
        }

        private sealed record PendingAlertCandidate(
            int InscriptionId,
            string AlertCode,
            string EventName,
            string StageStateSnapshot,
            string DeduplicationKey,
            int BusinessDaysElapsed,
            string? RecipientsSummary,
            Dictionary<string, object> EventData)
        {
            public static PendingAlertCandidate Create(
                int inscriptionId,
                string alertCode,
                string stageStateSnapshot,
                int businessDaysElapsed,
                string? recipientsSummary,
                Dictionary<string, object> eventData)
            {
                var dedupKey = $"{inscriptionId}|{alertCode}|{stageStateSnapshot}";
                return new PendingAlertCandidate(
                    inscriptionId,
                    alertCode,
                    alertCode,
                    stageStateSnapshot,
                    dedupKey,
                    businessDaysElapsed,
                    recipientsSummary,
                    eventData);
            }
        }
    }
}
