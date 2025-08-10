using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services
{
    /// <summary>
    /// Construye datos de eventos para notificaciones relacionadas con proyectos finales
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
                // Obtener el ProjectFinal
                var projectFinalRepo = _unitOfWork.GetRepository<ProjectFinal, int>();
                var projectFinal = await projectFinalRepo.GetByIdAsync(projectFinalId);
                
                if (projectFinal == null)
                {
                    _logger.LogWarning("ProjectFinal not found with ID: {ProjectFinalId}", projectFinalId);
                    return new Dictionary<string, object>();
                }

                // Obtener la InscriptionModality (el ID es el mismo)
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo.GetByIdAsync(projectFinalId);
                
                if (inscriptionModality == null)
                {
                    _logger.LogWarning("InscriptionModality not found with ID: {InscriptionModalityId}", projectFinalId);
                    return new Dictionary<string, object>();
                }

                // Verificar que sea modalidad PROYECTO_GRADO
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscriptionModality.IdModality);
                
                if (modality?.Code != "PROYECTO_GRADO")
                {
                    _logger.LogInformation("ProjectFinal is not for PROYECTO_GRADO modality (Modality: {Modality}). Skipping notification.", 
                        modality?.Code);
                    return new Dictionary<string, object>();
                }

                // Obtener datos del estudiante
                var (studentNames, studentEmails, studentCount) = 
                    await _studentDataService.GetStudentDataByInscriptionAsync(inscriptionModality.Id);

                // Obtener información del proyecto
                var projectTitle = await GetProjectTitleAsync(inscriptionModality);
                
                // Obtener información del estado actual
                var stateInfo = await GetStateInfoAsync(projectFinal.IdStateStage);

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
                    ["SubmissionDate"] = projectFinal.UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? 
                                       projectFinal.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    ["EventType"] = eventType,
                    ["Phase"] = "Proyecto Final",
                    
                    // Campos adicionales para templates de notificación
                    ["EvaluationResult"] = GetEvaluationResultText(stateInfo.StateCode),
                    ["Observations"] = projectFinal.Observations ?? "Sin observaciones registradas",
                    ["ReportApprovalDate"] = projectFinal.ReportApprovalDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                    ["FinalPhaseApprovalDate"] = projectFinal.FinalPhaseApprovalDate?.ToString("dd/MM/yyyy") ?? string.Empty
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

        /// <summary>
        /// Convierte el código de estado a texto de resultado de evaluación
        /// </summary>
        private string GetEvaluationResultText(string stateCode)
        {
            return stateCode switch
            {
                "PFINF_INFORME_APROBADO" => "APROBADO",
                "PFINF_INFORME_CON_OBSERVACIONES" => "CON OBSERVACIONES",
                "PFINF_RADICADO_EN_EVALUACION" => "EN EVALUACIÓN",
                "PFINF_PENDIENTE_INFORME" => "PENDIENTE INFORME",
                _ => "ESTADO DESCONOCIDO"
            };
        }
    }
}
