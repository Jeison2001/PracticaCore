using Application.Common.Helpers;
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
                var evaluationResult = GetEvaluationResultText(stateInfo.StateCode, stateInfo.IsRejectionState);
                var tone = GetTone(stateInfo.StateCode, stateInfo.IsRejectionState);
                var cleanObservations = string.IsNullOrWhiteSpace(presentation.Observations)
                    ? string.Empty
                    : presentation.Observations.Trim();

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
                        ? NotificationFormattingHelper.ToColombiaDateTime(presentation.ScheduledDateTime.Value)
                        : "Por definir",
                    ["ActualDateTime"] = NotificationFormattingHelper.ToColombiaDateTime(
                        presentation.ActualDateTime ?? DateTimeOffset.UtcNow),
                    ["PresentationModality"] = presentation.PresentationModality ?? "No especificada",
                    ["LocationOrLink"] = presentation.LocationOrLink ?? "No especificado",
                    ["AttemptNumber"] = presentation.AttemptNumber,
                    ["EvaluationResult"] = evaluationResult,
                    ["EvaluationBadgeHtml"] = NotificationFormattingHelper.BuildEvaluationBadgeHtml(evaluationResult, tone),
                    ["TopAccentColor"] = NotificationFormattingHelper.GetTopAccentColor(tone),
                    ["Observations"] = cleanObservations,
                    ["ObservationsBlock"] = NotificationFormattingHelper.BuildObservationsBlockHtml(cleanObservations, tone),
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

        private async Task<(string StateName, string StateCode, bool IsRejectionState)> GetStateInfoAsync(int stateStageId)
        {
            try
            {
                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await stateStageRepo.GetByIdAsync(stateStageId);

                return (stateStage?.Name ?? "Estado desconocido", stateStage?.Code ?? "UNKNOWN", stateStage?.IsRejectionState ?? false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state info for StateStage ID: {StateStageId}", stateStageId);
                return ("Estado desconocido", "UNKNOWN", false);
            }
        }

        private static string GetTone(string stateCode, bool isRejectionState)
        {
            var upper = (stateCode ?? string.Empty).ToUpperInvariant();
            if (upper == nameof(StateStageCodeEnum.SUST_APROBADA))
                return "approved";
            if (upper == nameof(StateStageCodeEnum.SUST_NO_APROBADA_REINTENTO))
                return "warning";
            if (isRejectionState || upper is nameof(StateStageCodeEnum.SUST_REPROBADA_FINAL) or "SUST_NO_APROBADA_DEFINITIVA")
                return "rejected";
            return "info";
        }

        private static string GetEvaluationResultText(string stateCode, bool isRejectionState)
        {
            var upper = (stateCode ?? string.Empty).ToUpperInvariant();
            return upper switch
            {
                nameof(StateStageCodeEnum.SUST_APROBADA) => "APROBADA",
                nameof(StateStageCodeEnum.SUST_NO_APROBADA_REINTENTO) => "NO APROBADA (CON REINTENTO)",
                nameof(StateStageCodeEnum.SUST_REPROBADA_FINAL) or "SUST_NO_APROBADA_DEFINITIVA" => "NO APROBADA DEFINITIVA (REPROBADA)",
                nameof(StateStageCodeEnum.SUST_PROGRAMADA) => "PROGRAMADA",
                nameof(StateStageCodeEnum.SUST_PENDIENTE_PROGRAMACION) => "PENDIENTE DE PROGRAMACIÓN",
                _ => isRejectionState ? "NO APROBADA DEFINITIVA (REPROBADA)" : "ESTADO DESCONOCIDO"
            };
        }
    }
}
