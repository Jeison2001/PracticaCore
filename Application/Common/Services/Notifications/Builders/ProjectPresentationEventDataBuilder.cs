using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    public class ProjectPresentationEventDataBuilder : IProjectPresentationEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<ProjectPresentationEventDataBuilder> _logger;

        private static readonly TimeZoneInfo ColombiaTimeZone = ResolveColombiaTimeZone();

        public ProjectPresentationEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<ProjectPresentationEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildProjectPresentationEventDataAsync(int presentationId, string eventType)
        {
            _logger.LogInformation("Building project presentation event data for ID: {PresentationId}, Event: {EventType}",
                presentationId, eventType);

            try
            {
                var presentationRepo = _unitOfWork.GetRepository<ProjectPresentation, int>();
                var presentation = await presentationRepo.GetByIdAsync(presentationId);

                if (presentation == null)
                {
                    _logger.LogWarning("ProjectPresentation not found with ID: {PresentationId}", presentationId);
                    return new Dictionary<string, object>();
                }

                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo.GetByIdAsync(presentationId);

                if (inscriptionModality == null)
                {
                    _logger.LogWarning("InscriptionModality not found with ID: {Id}", presentationId);
                    return new Dictionary<string, object>();
                }

                var (studentNames, studentEmails, studentCount) =
                    await _studentDataService.GetStudentDataByInscriptionAsync(inscriptionModality.Id);

                var projectTitle = await GetProjectTitleAsync(inscriptionModality);
                var stateInfo = await GetStateInfoAsync(presentation.IdStateStage);

                var eventData = new Dictionary<string, object>
                {
                    ["PresentationId"] = presentation.Id,
                    ["InscriptionModalityId"] = inscriptionModality.Id,
                    ["ProjectTitle"] = projectTitle,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentsCount"] = studentCount,
                    ["CurrentState"] = stateInfo.StateName,
                    ["CurrentStateCode"] = stateInfo.StateCode,
                    ["ScheduledDateTime"] = presentation.ScheduledDateTime.HasValue
                        ? FormatColombiaDateTime(presentation.ScheduledDateTime.Value)
                        : "Por definir",
                    ["ActualDateTime"] = FormatColombiaDateTime(presentation.ActualDateTime ?? DateTimeOffset.UtcNow),
                    ["PresentationModality"] = presentation.PresentationModality ?? "No especificada",
                    ["LocationOrLink"] = presentation.LocationOrLink ?? "No especificado",
                    ["AttemptNumber"] = presentation.AttemptNumber,
                    ["EvaluationResult"] = GetEvaluationResultText(stateInfo.StateCode),
                    ["Observations"] = !string.IsNullOrWhiteSpace(presentation.Observations) ? presentation.Observations : "Sin observaciones registradas",
                    ["EventType"] = eventType,
                    ["Phase"] = "Sustentación"
                };

                _logger.LogInformation("Project presentation event data built successfully for ID: {PresentationId}", presentationId);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building project presentation event data for ID: {PresentationId}", presentationId);
                return new Dictionary<string, object>();
            }
        }

        private async Task<string> GetProjectTitleAsync(InscriptionModality inscriptionModality)
        {
            try
            {
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetFirstOrDefaultAsync(
                    p => p.Id == inscriptionModality.Id && p.StatusRegister,
                    CancellationToken.None);

                return proposal?.Title ?? "Proyecto de Grado sin título";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project title for InscriptionModality ID: {Id}", inscriptionModality.Id);
                return "Proyecto de Grado sin título";
            }
        }

        private async Task<(string StateName, string StateCode)> GetStateInfoAsync(int stateStageId)
        {
            try
            {
                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await stateStageRepo.GetByIdAsync(stateStageId);

                return (stateStage?.Name ?? "Estado desconocido", stateStage?.Code ?? "UNKNOWN");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state info for StateStage ID: {StateStageId}", stateStageId);
                return ("Estado desconocido", "UNKNOWN");
            }
        }

        private static string FormatColombiaDateTime(DateTimeOffset value) =>
            TimeZoneInfo.ConvertTime(value, ColombiaTimeZone).ToString("dd/MM/yyyy HH:mm");

        private static TimeZoneInfo ResolveColombiaTimeZone()
        {
            foreach (var tzId in new[] { "America/Bogota", "SA Pacific Standard Time" })
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(tzId, out var tz))
                    return tz;
            }
            return TimeZoneInfo.CreateCustomTimeZone(
                "COT",
                TimeSpan.FromHours(-5),
                "Colombia Standard Time",
                "Colombia Standard Time");
        }

        private string GetEvaluationResultText(string stateCode)
        {
            if (Enum.TryParse<StateStageCodeEnum>(stateCode, out var code))
            {
                return code switch
                {
                    StateStageCodeEnum.SUST_APROBADA => "APROBADA",
                    StateStageCodeEnum.SUST_NO_APROBADA_REINTENTO => "NO APROBADA (CON REINTENTO)",
                    StateStageCodeEnum.SUST_REPROBADA_FINAL => "NO APROBADA DEFINITIVA (REPROBADA)",
                    StateStageCodeEnum.SUST_PROGRAMADA => "PROGRAMADA",
                    StateStageCodeEnum.SUST_PENDIENTE_PROGRAMACION => "PENDIENTE DE PROGRAMACIÓN",
                    _ => "ESTADO DESCONOCIDO"
                };
            }

            return "ESTADO DESCONOCIDO";
        }
    }
}
