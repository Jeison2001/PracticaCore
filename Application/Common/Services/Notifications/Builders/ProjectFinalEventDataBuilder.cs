using Application.Common.Helpers;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Builders
{
    /// <summary>
    /// Construye datos de notificación para ProjectFinal con la misma estructura que
    /// PreliminaryProjectEventDataBuilder (título, estudiantes, estado, fechas, observaciones).
    /// </summary>
    public class ProjectFinalEventDataBuilder : IProjectFinalEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<ProjectFinalEventDataBuilder> _logger;

        public ProjectFinalEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<ProjectFinalEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildProjectFinalEventDataAsync(int projectFinalId, string eventType)
        {
            _logger.LogInformation("Building project final event data for ProjectFinal ID: {ProjectFinalId}, Event: {EventType}", 
                projectFinalId, eventType);

            try
            {
                var projectFinalRepo = _unitOfWork.GetRepository<ProjectFinal, int>();
                var projectFinal = await projectFinalRepo.GetByIdAsync(projectFinalId);

                if (projectFinal == null)
                {
                    _logger.LogWarning("ProjectFinal not found with ID: {ProjectFinalId}", projectFinalId);
                    return new Dictionary<string, object>();
                }

                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo.GetByIdAsync(projectFinalId);

                if (inscriptionModality == null)
                {
                    _logger.LogWarning("InscriptionModality not found with ID: {InscriptionModalityId}", projectFinalId);
                    return new Dictionary<string, object>();
                }

                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscriptionModality.IdModality);

                if (modality?.Code != "PROYECTO_GRADO")
                {
                    _logger.LogInformation("ProjectFinal is not for PROYECTO_GRADO modality (Modality: {Modality}). Skipping notification.",
                        modality?.Code);
                    return new Dictionary<string, object>();
                }

                var (studentNames, studentEmails, studentCount) =
                    await _studentDataService.GetStudentDataByInscriptionAsync(inscriptionModality.Id);

                var projectTitle = await GetProjectTitleAsync(inscriptionModality);

                var stateInfo = await GetStateInfoAsync(projectFinal.IdStateStage);
                var evaluationResult = GetEvaluationResultText(stateInfo.StateCode, stateInfo.IsRejectionState);
                var tone = GetTone(stateInfo.StateCode, stateInfo.IsRejectionState);
                var cleanObservations = string.IsNullOrWhiteSpace(projectFinal.Observations)
                    ? string.Empty
                    : projectFinal.Observations.Trim();

                var eventData = new Dictionary<string, object>
                {
                    ["ProjectFinalId"] = projectFinal.Id,
                    ["InscriptionModalityId"] = inscriptionModality.Id,
                    ["ProjectTitle"] = projectTitle,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentsCount"] = studentCount,
                    ["CurrentState"] = stateInfo.StateName,
                    ["CurrentStateCode"] = stateInfo.StateCode,
                    ["SubmissionDate"] = NotificationFormattingHelper.ToColombiaDateTime(
                        projectFinal.UpdatedAt ?? projectFinal.CreatedAt),
                    ["EventType"] = eventType,
                    ["Phase"] = "Proyecto Final",

                    ["EvaluationResult"] = evaluationResult,
                    ["EvaluationBadgeHtml"] = NotificationFormattingHelper.BuildEvaluationBadgeHtml(evaluationResult, tone),
                    ["TopAccentColor"] = NotificationFormattingHelper.GetTopAccentColor(tone),
                    ["Observations"] = cleanObservations,
                    ["ObservationsBlock"] = NotificationFormattingHelper.BuildObservationsBlockHtml(cleanObservations, tone),
                    ["ReportApprovalDate"] = NotificationFormattingHelper.ToColombiaDate(projectFinal.ReportApprovalDate),
                    ["FinalPhaseApprovalDate"] = NotificationFormattingHelper.ToColombiaDate(projectFinal.FinalPhaseApprovalDate)
                };

                _logger.LogInformation("Project final event data built successfully for ProjectFinal ID: {ProjectFinalId}", projectFinalId);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building project final event data for ProjectFinal ID: {ProjectFinalId}", projectFinalId);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Obtiene el título del proyecto de la propuesta asociada
        /// </summary>
        private async Task<string> GetProjectTitleAsync(InscriptionModality inscriptionModality)
        {
            try
            {
                // Buscar la propuesta asociada a esta inscripción
                var proposalRepo = _unitOfWork.GetRepository<Proposal, int>();
                var proposal = await proposalRepo.GetFirstOrDefaultAsync(
                    p => p.Id == inscriptionModality.Id && p.StatusRegister,
                    CancellationToken.None);

                return proposal?.Title ?? "Proyecto Final sin título";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project title for InscriptionModality ID: {InscriptionModalityId}", 
                    inscriptionModality.Id);
                return "Proyecto Final sin título";
            }
        }

        /// <summary>
        /// Obtiene información del estado actual
        /// </summary>
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
            if (upper is nameof(StateStageCodeEnum.PFINF_INFORME_APROBADO) or "PFINF_APROBADO")
                return "approved";
            if (upper is nameof(StateStageCodeEnum.PFINF_INFORME_CON_OBSERVACIONES) or "PFINF_CON_OBSERVACIONES")
                return "warning";
            if (isRejectionState || upper is "PFINF_RECHAZADO" or "PFINF_INFORME_RECHAZADO" || upper.Contains("RECHAZ"))
                return "rejected";
            return "info";
        }

        /// <summary>
        /// Convierte el código de estado a texto de resultado de evaluación
        /// </summary>
        private static string GetEvaluationResultText(string stateCode, bool isRejectionState)
        {
            var upper = (stateCode ?? string.Empty).ToUpperInvariant();
            return upper switch
            {
                nameof(StateStageCodeEnum.PFINF_INFORME_APROBADO) or "PFINF_APROBADO" => "APROBADO",
                nameof(StateStageCodeEnum.PFINF_INFORME_CON_OBSERVACIONES) or "PFINF_CON_OBSERVACIONES" => "CON OBSERVACIONES",
                nameof(StateStageCodeEnum.PFINF_RADICADO_EN_EVALUACION) => "EN EVALUACIÓN",
                nameof(StateStageCodeEnum.PFINF_PENDIENTE_INFORME) => "PENDIENTE INFORME",
                "PFINF_RECHAZADO" or "PFINF_INFORME_RECHAZADO" => "RECHAZADO",
                _ => isRejectionState ? "RECHAZADO" : "ESTADO DESCONOCIDO"
            };
        }
    }
}
