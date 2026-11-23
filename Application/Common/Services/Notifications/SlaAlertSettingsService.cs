using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Application.Common.Helpers;
using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Microsoft.Extensions.Configuration;

namespace Application.Common.Services.Notifications
{
    public class SlaAlertSettingsService : ISlaAlertSettingsService
    {
        private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly Regex NewlineRegex = new(@"\r?\n", RegexOptions.Compiled);

        private static readonly HashSet<string> RawHtmlKeys = new(StringComparer.Ordinal)
        {
            "NextSteps",
            "PossibleCauses",
            "AvailableOptions",
            "ObservationsBlock",
            "EvaluationBadgeHtml",
            "TopAccentColor",
            "SpecificObjectivesHtml"
        };

        public static readonly IReadOnlyList<SlaAlertCcOptionDto> CatalogCcOptions =
        [
            new SlaAlertCcOptionDto
            {
                Key = "REL:DIRECTOR_ASSIGNED",
                RuleType = "BY_ENTITY_RELATION",
                RuleValue = "DIRECTOR_ASSIGNED",
                Label = "Director de proyecto / modalidad",
                Category = "Relación académica"
            },
            new SlaAlertCcOptionDto
            {
                Key = "REL:CO_DIRECTOR_ASSIGNED",
                RuleType = "BY_ENTITY_RELATION",
                RuleValue = "CO_DIRECTOR_ASSIGNED",
                Label = "Codirector asignado",
                Category = "Relación académica"
            },
            new SlaAlertCcOptionDto
            {
                Key = "REL:ASESOR_ASSIGNED",
                RuleType = "BY_ENTITY_RELATION",
                RuleValue = "ASESOR_ASSIGNED",
                Label = "Asesor asignado",
                Category = "Relación académica"
            },
            new SlaAlertCcOptionDto
            {
                Key = "ROLE:COMMITTEE_DIRECTOR",
                RuleType = "BY_ROLE",
                RuleValue = "COMMITTEE_DIRECTOR",
                Label = "Dirección de Comité",
                Category = "Cargo institucional"
            },
            new SlaAlertCcOptionDto
            {
                Key = "ROLE:SECRETARY",
                RuleType = "BY_ROLE",
                RuleValue = "SECRETARY",
                Label = "Secretaría Académica",
                Category = "Cargo institucional"
            },
            new SlaAlertCcOptionDto
            {
                Key = "ROLE:DEPARTMENT_HEAD",
                RuleType = "BY_ROLE",
                RuleValue = "DEPARTMENT_HEAD",
                Label = "Jefatura de Departamento",
                Category = "Cargo institucional"
            }
        ];

        private static readonly Dictionary<string, SlaAlertCcOptionDto> CcOptionsByKey =
            CatalogCcOptions.ToDictionary(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);

        private static readonly string[] OrderedSlaEvents =
        [
            SlaDeadlineAlertService.AlertEvaluatorApproaching,
            SlaDeadlineAlertService.AlertEvaluatorOverdue,
            SlaDeadlineAlertService.AlertCommitteeOverdue,
            SlaDeadlineAlertService.AlertDefenseReminder,
            SlaDeadlineAlertService.AlertStudentObservations,
            SlaDeadlineAlertService.AlertTermLimitWarning
        ];

        private static readonly HashSet<string> AllowedSlaEvents =
            new(OrderedSlaEvents, StringComparer.OrdinalIgnoreCase);

        private readonly ISlaAlertRepository _slaAlertRepository;
        private readonly ISlaRecurringJobScheduler _recurringJobScheduler;
        private readonly IConfiguration? _configuration;
        private readonly TimeProvider _timeProvider;

        public SlaAlertSettingsService(
            ISlaAlertRepository slaAlertRepository,
            ISlaRecurringJobScheduler recurringJobScheduler,
            IConfiguration? configuration = null,
            TimeProvider? timeProvider = null)
        {
            _slaAlertRepository = slaAlertRepository;
            _recurringJobScheduler = recurringJobScheduler;
            _configuration = configuration;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<SlaAlertSettingsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            var snapshot = await _slaAlertRepository.GetSettingsSnapshotAsync(OrderedSlaEvents, cancellationToken);
            return BuildOverviewDto(snapshot);
        }

        public async Task<(SlaAlertSettingsOverviewDto? Overview, List<string> ValidationErrors)> UpdateSettingsAsync(
            UpdateSlaAlertSettingsRequest? request,
            int? currentUserId,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return (null, ["La solicitud de configuración de alertas SLA no puede ser nula."]);
            }

            if (request.Global != null)
            {
                request.IsEnabled = request.Global.IsEnabled;
                request.ScheduleHourColombia = request.Global.ScheduleHourColombia;
                request.ScheduleMinuteColombia = request.Global.ScheduleMinuteColombia;
                request.WeekdaysOnly = request.Global.WeekdaysOnly;
                request.EvaluatorWarningBusinessDays = request.Global.EvaluatorWarningBusinessDays;
                request.EvaluatorOverdueBusinessDays = request.Global.EvaluatorOverdueBusinessDays;
                request.CommitteeOverdueBusinessDays = request.Global.CommitteeOverdueBusinessDays;
                request.StudentObservationsBusinessDays = request.Global.StudentObservationsBusinessDays;
                request.DefenseReminderHours = request.Global.DefenseReminderHours;
                request.WarnOnLastAllowedPeriod = request.Global.WarnOnLastAllowedPeriod;
            }

            var errors = ValidateRequest(request);
            if (errors.Count > 0)
            {
                return (null, errors);
            }

            var updatedGlobal = new SlaAlertConfiguration
            {
                Id = 1,
                IsEnabled = request.IsEnabled,
                ScheduleHourColombia = request.ScheduleHourColombia,
                ScheduleMinuteColombia = request.ScheduleMinuteColombia,
                WeekdaysOnly = request.WeekdaysOnly,
                EvaluatorWarningBusinessDays = request.EvaluatorWarningBusinessDays,
                EvaluatorOverdueBusinessDays = request.EvaluatorOverdueBusinessDays,
                CommitteeOverdueBusinessDays = request.CommitteeOverdueBusinessDays,
                StudentObservationsBusinessDays = request.StudentObservationsBusinessDays,
                DefenseReminderHours = request.DefenseReminderHours,
                WarnOnLastAllowedPeriod = request.WarnOnLastAllowedPeriod
            };

            var instructions = new List<SlaAlertRulePersistenceInstruction>();
            foreach (var ruleReq in request.Rules!)
            {
                var canonicalEvent = OrderedSlaEvents.First(e =>
                    string.Equals(e, ruleReq.EventName.Trim(), StringComparison.OrdinalIgnoreCase));

                var distinctCcTargets = new List<SlaAlertCcTarget>();
                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (ruleReq.SelectedCcKeys != null)
                {
                    foreach (var rawKey in ruleReq.SelectedCcKeys)
                    {
                        var trimmedKey = rawKey.Trim();
                        if (seenKeys.Add(trimmedKey))
                        {
                            var ccOpt = CcOptionsByKey[trimmedKey];
                            distinctCcTargets.Add(new SlaAlertCcTarget(ccOpt.RuleType, ccOpt.RuleValue));
                        }
                    }
                }

                var (defaultSubject, defaultBody, defaultToRules) = GetDefaultTemplateAndToRules(canonicalEvent);
                instructions.Add(new SlaAlertRulePersistenceInstruction(
                    canonicalEvent,
                    ruleReq.IsActive,
                    distinctCcTargets,
                    defaultSubject,
                    defaultBody,
                    defaultToRules));
            }

            var nowUtc = _timeProvider.GetUtcNow();
            await _slaAlertRepository.SaveSettingsAsync(
                updatedGlobal,
                instructions,
                currentUserId,
                nowUtc,
                cancellationToken);

            _recurringJobScheduler.SyncRecurringJob(
                updatedGlobal.IsEnabled,
                updatedGlobal.ScheduleHourColombia,
                updatedGlobal.ScheduleMinuteColombia,
                updatedGlobal.WeekdaysOnly);

            var overview = await GetOverviewAsync(cancellationToken);
            return (overview, []);
        }

        public Task<SlaAlertDispatchLogsPageDto> GetLogsAsync(
            int page,
            int pageSize,
            string? alertCode,
            string? search,
            CancellationToken cancellationToken = default) =>
            _slaAlertRepository.GetDispatchLogsAsync(page, pageSize, alertCode, search, cancellationToken);

        private static List<string> ValidateRequest(UpdateSlaAlertSettingsRequest request)
        {
            var errors = new List<string>();

            if (request.ScheduleHourColombia < 0 || request.ScheduleHourColombia > 23)
            {
                errors.Add("La hora de ejecución programada en Colombia debe estar entre 0 y 23.");
            }

            if (request.ScheduleMinuteColombia < 0 || request.ScheduleMinuteColombia > 59)
            {
                errors.Add("El minuto de ejecución programada en Colombia debe estar entre 0 y 59.");
            }

            if (request.EvaluatorWarningBusinessDays < 1 || request.EvaluatorWarningBusinessDays > 89)
            {
                errors.Add("El umbral preventivo de evaluación debe estar entre 1 y 89 días hábiles.");
            }

            if (request.EvaluatorOverdueBusinessDays < 2 || request.EvaluatorOverdueBusinessDays > 90)
            {
                errors.Add("El plazo máximo de evaluación vencida debe estar entre 2 y 90 días hábiles.");
            }

            if (request.EvaluatorWarningBusinessDays >= request.EvaluatorOverdueBusinessDays)
            {
                errors.Add("El umbral preventivo de evaluación debe ser estrictamente menor que el plazo máximo de evaluación vencida.");
            }

            if (request.CommitteeOverdueBusinessDays < 1 || request.CommitteeOverdueBusinessDays > 90)
            {
                errors.Add("El umbral de gestión pendiente en Comité debe estar entre 1 y 90 días hábiles.");
            }

            if (request.StudentObservationsBusinessDays < 1 || request.StudentObservationsBusinessDays > 90)
            {
                errors.Add("El umbral de recordatorio de observaciones al estudiante debe estar entre 1 y 90 días hábiles.");
            }

            if (request.DefenseReminderHours < 1 || request.DefenseReminderHours > 168)
            {
                errors.Add("La ventana de recordatorio previo de sustentación debe estar entre 1 y 168 horas.");
            }

            if (request.Rules == null)
            {
                errors.Add("Debe especificar la configuración de reglas de alerta SLA.");
                return errors;
            }

            var seenEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in request.Rules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.EventName))
                {
                    errors.Add("Cada regla de alerta debe indicar un código de evento válido.");
                    continue;
                }

                var trimmedEvent = rule.EventName.Trim();
                if (!AllowedSlaEvents.Contains(trimmedEvent))
                {
                    errors.Add($"El evento '{trimmedEvent}' no pertenece al catálogo de alertas SLA permitidas.");
                    continue;
                }

                if (!seenEvents.Add(trimmedEvent))
                {
                    errors.Add($"El evento '{trimmedEvent}' está duplicado en la solicitud.");
                }

                if (rule.SelectedCcKeys == null)
                    continue;

                foreach (var ccKey in rule.SelectedCcKeys)
                {
                    if (string.IsNullOrWhiteSpace(ccKey) || !CcOptionsByKey.ContainsKey(ccKey.Trim()))
                    {
                        errors.Add($"El destinatario en copia '{ccKey}' no pertenece al catálogo institucional permitido.");
                    }
                }
            }

            return errors;
        }

        private SlaAlertSettingsOverviewDto BuildOverviewDto(SlaAlertSettingsSnapshot snapshot)
        {
            var cfg = snapshot.Configuration;
            var isEnabled = cfg?.IsEnabled ?? GetBoolConfig("SlaAlerts:Enabled", true);
            var hour = cfg?.ScheduleHourColombia ?? 7;
            var minute = cfg?.ScheduleMinuteColombia ?? 30;
            var weekdaysOnly = cfg?.WeekdaysOnly ?? false;
            var evalWarning = cfg?.EvaluatorWarningBusinessDays ?? GetIntConfig("SlaAlerts:EvaluatorWarningBusinessDays", 20);
            var evalOverdue = cfg?.EvaluatorOverdueBusinessDays ?? GetIntConfig("SlaAlerts:EvaluatorOverdueBusinessDays", 25);
            var committeeOverdue = cfg?.CommitteeOverdueBusinessDays ?? GetIntConfig("SlaAlerts:CommitteeOverdueBusinessDays", 10);
            var studentObs = cfg?.StudentObservationsBusinessDays ?? GetIntConfig("SlaAlerts:StudentObservationsBusinessDays", 15);
            var defenseHours = cfg?.DefenseReminderHours ?? GetIntConfig("SlaAlerts:DefenseReminderHours", 48);
            var warnLastPeriod = cfg?.WarnOnLastAllowedPeriod ?? GetBoolConfig("SlaAlerts:WarnOnLastAllowedPeriod", true);

            var globalDto = new SlaAlertGlobalSettingsDto
            {
                IsEnabled = isEnabled,
                ScheduleHourColombia = hour,
                ScheduleMinuteColombia = minute,
                WeekdaysOnly = weekdaysOnly,
                CronExpression = SlaCronHelper.BuildCron(hour, minute, weekdaysOnly),
                EvaluatorWarningBusinessDays = evalWarning,
                EvaluatorOverdueBusinessDays = evalOverdue,
                CommitteeOverdueBusinessDays = committeeOverdue,
                StudentObservationsBusinessDays = studentObs,
                DefenseReminderHours = defenseHours,
                WarnOnLastAllowedPeriod = warnLastPeriod,
                LastRunAtUtc = cfg?.LastRunAt?.ToUniversalTime(),
                LastRunAtColombia = cfg?.LastRunAt.HasValue == true
                    ? DashboardTimeHelper.FormatDateTime(cfg.LastRunAt.Value)
                    : null,
                LastRunEvaluatedCount = cfg?.LastRunEvaluatedCount,
                LastRunTriggeredCount = cfg?.LastRunTriggeredCount,
                LastRunSkippedCount = cfg?.LastRunSkippedCount,
                LastRunFailedCount = cfg?.LastRunFailedCount,
                UpdatedAtUtc = cfg != null
                    ? (cfg.UpdatedAt ?? cfg.CreatedAt).ToUniversalTime()
                    : null,
                UpdatedAtColombia = cfg != null
                    ? DashboardTimeHelper.FormatDateTime(cfg.UpdatedAt ?? cfg.CreatedAt)
                    : null
            };

            var configsByEvent = snapshot.Configs
                .GroupBy(c => c.EventName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First(), StringComparer.OrdinalIgnoreCase);

            var ccByConfigId = snapshot.ActiveCcRules
                .GroupBy(r => r.EmailNotificationConfigId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rulesList = new List<SlaAlertRuleSettingDto>(OrderedSlaEvents.Length);
            foreach (var eventName in OrderedSlaEvents)
            {
                configsByEvent.TryGetValue(eventName, out var dbEmailConfig);
                var selectedCcKeys = new List<string>();

                if (dbEmailConfig != null && ccByConfigId.TryGetValue(dbEmailConfig.Id, out var activeCcList))
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var cc in activeCcList)
                    {
                        var mappedKey = MapRuleToCcKey(cc.RuleType, cc.RuleValue);
                        if (mappedKey != null && seen.Add(mappedKey))
                        {
                            selectedCcKeys.Add(mappedKey);
                        }
                    }
                }

                var meta = GetRuleMetadata(
                    eventName,
                    evalWarning,
                    evalOverdue,
                    committeeOverdue,
                    defenseHours,
                    studentObs,
                    warnLastPeriod);

                var (defaultSubject, defaultBody, _) = GetDefaultTemplateAndToRules(eventName);
                var subjectTemplate = !string.IsNullOrWhiteSpace(dbEmailConfig?.SubjectTemplate)
                    ? dbEmailConfig!.SubjectTemplate
                    : defaultSubject;
                var bodyTemplate = !string.IsNullOrWhiteSpace(dbEmailConfig?.BodyTemplate)
                    ? dbEmailConfig!.BodyTemplate
                    : defaultBody;

                var sampleData = BuildSampleEventData(
                    eventName,
                    evalWarning,
                    evalOverdue,
                    committeeOverdue,
                    defenseHours,
                    studentObs,
                    warnLastPeriod);

                rulesList.Add(new SlaAlertRuleSettingDto
                {
                    EventName = eventName,
                    Title = meta.Title,
                    Description = meta.Description,
                    NormativeReference = meta.NormativeReference,
                    ThresholdLabel = meta.ThresholdLabel,
                    PrimaryRecipientsLabel = meta.PrimaryRecipientsLabel,
                    PrimaryResolutionMode = meta.PrimaryResolutionMode,
                    IsActive = dbEmailConfig?.StatusRegister ?? true,
                    SelectedCcKeys = selectedCcKeys,
                    SampleSubject = RenderSubjectPreview(subjectTemplate, sampleData),
                    SampleHtml = RenderBodyPreview(bodyTemplate, sampleData)
                });
            }

            return new SlaAlertSettingsOverviewDto
            {
                Global = globalDto,
                AvailableCcOptions = CatalogCcOptions
                    .Select(o => new SlaAlertCcOptionDto
                    {
                        Key = o.Key,
                        RuleType = o.RuleType,
                        RuleValue = o.RuleValue,
                        Label = o.Label,
                        Category = o.Category
                    })
                    .ToList(),
                Rules = rulesList,
                ModalityTermLimits = snapshot.ModalityTermLimits
            };
        }

        private static string? MapRuleToCcKey(string? ruleType, string? ruleValue)
        {
            if (string.IsNullOrWhiteSpace(ruleType) || string.IsNullOrWhiteSpace(ruleValue))
                return null;

            var candidate = ruleType.Trim().ToUpperInvariant() switch
            {
                "BY_ENTITY_RELATION" => $"REL:{ruleValue.Trim().ToUpperInvariant()}",
                "BY_ROLE" => $"ROLE:{ruleValue.Trim().ToUpperInvariant()}",
                _ => null
            };

            return candidate != null && CcOptionsByKey.TryGetValue(candidate, out var canonical)
                ? canonical.Key
                : null;
        }

        private Dictionary<string, object> BuildSampleEventData(
            string eventName,
            int evalWarning,
            int evalOverdue,
            int committeeOverdue,
            int defenseHours,
            int studentObs,
            bool warnLastPeriod)
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var isOverdue = string.Equals(eventName, SlaDeadlineAlertService.AlertEvaluatorOverdue, StringComparison.OrdinalIgnoreCase);
            var isDefense = string.Equals(eventName, SlaDeadlineAlertService.AlertDefenseReminder, StringComparison.OrdinalIgnoreCase);
            var isTerm = string.Equals(eventName, SlaDeadlineAlertService.AlertTermLimitWarning, StringComparison.OrdinalIgnoreCase);

            var accent = isOverdue || (isTerm && !warnLastPeriod)
                ? "#DC2626"
                : isDefense
                    ? "#006633"
                    : "#D97706";

            var elapsedDays = eventName switch
            {
                SlaDeadlineAlertService.AlertEvaluatorApproaching => evalWarning,
                SlaDeadlineAlertService.AlertEvaluatorOverdue => evalOverdue + 2,
                SlaDeadlineAlertService.AlertCommitteeOverdue => committeeOverdue + 1,
                SlaDeadlineAlertService.AlertStudentObservations => studentObs + 1,
                _ => evalWarning
            };

            var remainingDays = Math.Max(1, evalOverdue - evalWarning);
            var sampleActivityDate = NotificationFormattingHelper.ToColombiaDate(nowUtc.AddDays(-Math.Max(7, elapsedDays + 4)));
            var sampleDefenseDateTime = NotificationFormattingHelper.ToColombiaDateTime(nowUtc.AddHours(Math.Min(24, defenseHours)));
            var periodsElapsed = warnLastPeriod ? 3 : 4;
            var maxTermPeriods = 3;
            var riskStatusLabel = warnLastPeriod
                ? "Último periodo académico permitido"
                : "Plazo reglamentario excedido";
            var normativeGuidance = warnLastPeriod
                ? "Conforme al Acuerdo No. 015 de 2021 (Artículos 7 y 8), usted cursa actualmente el último periodo académico ordinario permitido para culminar su modalidad de grado."
                : "Conforme al Acuerdo No. 015 de 2021 (Artículos 7 y 8), la inscripción ha superado el número máximo de periodos académicos ordinarios permitidos para la modalidad.";

            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["AppUrl"] = NotificationFormattingHelper.ResolveAppBaseUrl(_configuration),
                ["LoginUrl"] = NotificationFormattingHelper.ResolveLoginUrl(_configuration),
                ["TopAccentColor"] = accent,
                ["WorkTitle"] = "Sistema predictivo de deserción estudiantil mediante analítica de datos",
                ["ModalityName"] = "Proyecto de Grado",
                ["StudentNames"] = "Valeria Mendoza Gómez, Andrés Felipe Ríos",
                ["StudentEmails"] = "vmendoza@unicesar.edu.co, afrios@unicesar.edu.co",
                ["AcademicPeriod"] = "2026-1",
                ["CurrentStageName"] = isDefense ? "Sustentación" : "Anteproyecto",
                ["CurrentStateName"] = isDefense
                    ? "Sustentación Programada"
                    : eventName == SlaDeadlineAlertService.AlertStudentObservations
                        ? "Anteproyecto con Observaciones"
                        : eventName == SlaDeadlineAlertService.AlertCommitteeOverdue
                            ? "Anteproyecto Radicado Pendiente Jurados"
                            : "Anteproyecto en Evaluación",
                ["ActivitySinceDate"] = sampleActivityDate,
                ["BusinessDaysElapsed"] = elapsedDays,
                ["MaxBusinessDays"] = evalOverdue,
                ["RemainingBusinessDays"] = remainingDays,
                ["NormativeReference"] = $"Acuerdo No. 015 de 2021, Artículo 15 (máximo {evalOverdue} días hábiles)",
                ["PendingActionDescription"] = "Asignación de jurados evaluadores de anteproyecto",
                ["ScheduledDateTime"] = sampleDefenseDateTime,
                ["PresentationModality"] = "Presencial",
                ["LocationOrLink"] = "Auditorio Julio Villazón Baquero — Sede Sabanas",
                ["AttemptNumber"] = 1,
                ["JuryNames"] = "Ing. Carlos Ríos, M.Sc. María Fernanda Díaz",
                ["ObservationsBlock"] = NotificationFormattingHelper.BuildObservationsBlockHtml(
                    "1. Precisar las métricas de validación cruzada en el capítulo metodológico.\n2. Actualizar el cronograma de ejecución conforme al calendario académico vigente.",
                    "warning"),
                ["EvaluationBadgeHtml"] = NotificationFormattingHelper.BuildEvaluationBadgeHtml("Seguimiento SLA", "warning"),
                ["StartAcademicPeriod"] = "2025-1",
                ["PeriodsElapsed"] = periodsElapsed,
                ["MaxTermPeriods"] = maxTermPeriods,
                ["RiskStatusLabel"] = riskStatusLabel,
                ["NormativeGuidance"] = normativeGuidance
            };
        }

        private static string RenderSubjectPreview(string template, Dictionary<string, object> data) =>
            PlaceholderRegex.Replace(template ?? string.Empty, match =>
            {
                var key = match.Groups[1].Value;
                if (!data.TryGetValue(key, out var rawObj) || rawObj == null)
                    return string.Empty;

                return (rawObj.ToString() ?? string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();
            });

        private static string RenderBodyPreview(string template, Dictionary<string, object> data) =>
            PlaceholderRegex.Replace(template ?? string.Empty, match =>
            {
                var key = match.Groups[1].Value;
                if (!data.TryGetValue(key, out var rawObj) || rawObj == null)
                    return string.Empty;

                var raw = rawObj.ToString() ?? string.Empty;
                if (RawHtmlKeys.Contains(key))
                    return raw;

                var encoded = WebUtility.HtmlEncode(raw);
                return NewlineRegex.Replace(encoded, "<br />");
            });

        private static RuleMetadata GetRuleMetadata(
            string eventName,
            int evalWarning,
            int evalOverdue,
            int committeeOverdue,
            int defenseHours,
            int studentObs,
            bool warnLastPeriod) =>
            eventName switch
            {
                SlaDeadlineAlertService.AlertEvaluatorApproaching => new RuleMetadata(
                    "Aviso preventivo a evaluadores y jurados",
                    "Recordatorio automático previo al vencimiento del término reglamentario para registrar dictamen sobre anteproyecto, informe final o práctica académica.",
                    "Acuerdo 015 de 2021, Arts. 15 y 24",
                    $"Desde {evalWarning} hasta {Math.Max(evalWarning, evalOverdue - 1)} días hábiles en evaluación",
                    "Docente evaluador o jurados confirmados de la etapa (sin respaldo artificial)",
                    "BY_ENTITY_RELATION"),

                SlaDeadlineAlertService.AlertEvaluatorOverdue => new RuleMetadata(
                    "Vencimiento de plazo reglamentario de evaluación",
                    "Alerta prioritaria emitida cuando la evaluación docente o de jurados alcanza o supera el límite máximo de días hábiles.",
                    "Acuerdo 015 de 2021, Arts. 15 y 24",
                    $"A partir de {evalOverdue} días hábiles en evaluación",
                    "Docente evaluador o jurados confirmados de la etapa (sin respaldo artificial)",
                    "BY_ENTITY_RELATION"),

                SlaDeadlineAlertService.AlertCommitteeOverdue => new RuleMetadata(
                    "Gestión o revisión pendiente en Comité",
                    "Notifica expedientes radicados pendientes de aval inicial, pertinencia, asignación de jurados, programación de sustentación o dictamen curricular.",
                    "Acuerdo 015 de 2021 — Gestión curricular",
                    $"A partir de {committeeOverdue} días hábiles sin actuación",
                    "Responsable(s) del Comité con permiso de revisión/gestión de la etapa",
                    "BY_PERMISSION (etapa/modalidad)"),

                SlaDeadlineAlertService.AlertDefenseReminder => new RuleMetadata(
                    "Recordatorio previo de sustentación pública",
                    "Convocatoria recordatoria previa a la fecha y hora programadas para sustentación pública de Proyecto de Grado.",
                    "Acuerdo 015 de 2021, Art. 16",
                    $"Dentro de las {defenseHours} horas previas a la sustentación",
                    "Estudiante(s) sustentante(s) y jurados evaluadores asignados",
                    "BY_ENTITY_RELATION"),

                SlaDeadlineAlertService.AlertStudentObservations => new RuleMetadata(
                    "Recordatorio al estudiante por observaciones pendientes",
                    "Recuerda a los estudiantes subsanar correcciones u observaciones emitidas sobre su propuesta, anteproyecto, informe o modalidad.",
                    "Seguimiento académico SIGEPRO",
                    $"A partir de {studentObs} días hábiles con observaciones abiertas",
                    "Estudiante(s) vinculado(s) a la inscripción",
                    "BY_ENTITY_RELATION"),

                _ => new RuleMetadata(
                    "Control de plazo máximo por periodos académicos",
                    "Advierte sobre inscripciones que cursan su último periodo académico ordinario permitido o que superan el tope reglamentario de la modalidad.",
                    "Acuerdo 015 de 2021, Arts. 7 y 8",
                    warnLastPeriod
                        ? "Último periodo permitido (preventivo) y plazo excedido"
                        : "Únicamente cuando se excede el máximo de periodos",
                    "Estudiante(s) vinculado(s) a la inscripción",
                    "BY_ENTITY_RELATION")
            };

        private static (string Subject, string Body, IReadOnlyList<SlaAlertDefaultToRule> ToRules) GetDefaultTemplateAndToRules(string eventName) =>
            eventName switch
            {
                SlaDeadlineAlertService.AlertEvaluatorApproaching => (
                    "SIGEPRO · Recordatorio de plazo de evaluación ({RemainingBusinessDays} días hábiles restantes): {WorkTitle}",
                    BuildFallbackCardHtml(
                        "Recordatorio preventivo de plazo de evaluación",
                        "Restan <strong>{RemainingBusinessDays} días hábiles</strong> ({BusinessDaysElapsed} de {MaxBusinessDays}) para registrar evaluación de <strong>{WorkTitle}</strong> ({ModalityName}) — {NormativeReference}."),
                    [new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "STAGE_EVALUATOR_RESPONSIBLE", 1)]),

                SlaDeadlineAlertService.AlertEvaluatorOverdue => (
                    "SIGEPRO · Plazo reglamentario de evaluación vencido ({BusinessDaysElapsed} días hábiles): {WorkTitle}",
                    BuildFallbackCardHtml(
                        "Plazo reglamentario de evaluación vencido",
                        "Se registran <strong>{BusinessDaysElapsed} días hábiles</strong> acumulados (máx. {MaxBusinessDays}) desde {ActivitySinceDate} en <strong>{WorkTitle}</strong> ({ModalityName}) — {NormativeReference}."),
                    [new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "STAGE_EVALUATOR_RESPONSIBLE", 1)]),

                SlaDeadlineAlertService.AlertCommitteeOverdue => (
                    "SIGEPRO · Gestión pendiente en Comité ({BusinessDaysElapsed} días hábiles): {ModalityName} - {StudentNames}",
                    BuildFallbackCardHtml(
                        "Trámite pendiente de gestión por el Comité",
                        "Acción requerida: <strong>{PendingActionDescription}</strong> ({BusinessDaysElapsed} días hábiles desde {ActivitySinceDate}) para {StudentNames} en {ModalityName}."),
                    [new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "COMMITTEE_STAGE_RESPONSIBLE", 1)]),

                SlaDeadlineAlertService.AlertDefenseReminder => (
                    "SIGEPRO · Recordatorio de sustentación próxima ({ScheduledDateTime}): {WorkTitle}",
                    BuildFallbackCardHtml(
                        "Recordatorio oficial de sustentación pública",
                        "Sustentación programada el <strong>{ScheduledDateTime}</strong> ({PresentationModality} — {LocationOrLink}), intento #{AttemptNumber}. Jurados: {JuryNames}."),
                    [
                        new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "STUDENT_ASSIGNED", 1),
                        new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "JURY_EVALUATOR_ASSIGNED", 2)
                    ]),

                SlaDeadlineAlertService.AlertStudentObservations => (
                    "SIGEPRO · Recordatorio de correcciones pendientes ({BusinessDaysElapsed} días hábiles): {ModalityName}",
                    BuildFallbackCardHtml(
                        "Recordatorio de subsanación de observaciones",
                        "Estimado(a) {StudentNames}, su trámite <strong>{WorkTitle}</strong> registra {BusinessDaysElapsed} días hábiles con observaciones pendientes.{ObservationsBlock}"),
                    [new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "STUDENT_ASSIGNED", 1)]),

                _ => (
                    "SIGEPRO · Alerta de plazo reglamentario ({RiskStatusLabel} {PeriodsElapsed}/{MaxTermPeriods} periodos): {ModalityName}",
                    BuildFallbackCardHtml(
                        "Alerta reglamentaria de plazo por periodos académicos",
                        "Estado: <strong>{RiskStatusLabel}</strong> ({PeriodsElapsed} de {MaxTermPeriods} periodos desde {StartAcademicPeriod}). {NormativeGuidance}"),
                    [new SlaAlertDefaultToRule("BY_ENTITY_RELATION", "STUDENT_ASSIGNED", 1)])
            };

        private static string BuildFallbackCardHtml(string heading, string summaryHtml) =>
            $"<!DOCTYPE html><html lang=\"es\"><body style=\"margin:0;padding:20px;background:#F3F5F4;font-family:'Segoe UI',Arial,sans-serif;\"><div style=\"max-width:600px;margin:0 auto;background:#FFFFFF;border:1px solid #E2E8F0;border-top:5px solid {{TopAccentColor}};border-radius:12px;overflow:hidden;\"><div style=\"background:#005A2B;padding:20px 24px;color:#FFFFFF;\"><strong style=\"font-size:18px;letter-spacing:1px;\">SIGEPRO · UPC</strong></div><div style=\"padding:24px;color:#1E293B;font-size:14px;line-height:1.6;\"><h2 style=\"margin:0 0 12px 0;font-size:18px;color:#0F172A;\">{heading}</h2><p style=\"margin:0 0 16px 0;\">{summaryHtml}</p><p style=\"margin:16px 0 0 0;\"><a href=\"{{LoginUrl}}\" target=\"_blank\" style=\"display:inline-block;background:#006633;color:#FFFFFF;text-decoration:none;padding:10px 20px;border-radius:8px;font-weight:600;\">Ingresar a SIGEPRO</a></p></div></div></body></html>";

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

        private sealed record RuleMetadata(
            string Title,
            string Description,
            string NormativeReference,
            string ThresholdLabel,
            string PrimaryRecipientsLabel,
            string PrimaryResolutionMode);
    }
}
