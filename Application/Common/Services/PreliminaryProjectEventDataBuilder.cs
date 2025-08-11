using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services
{
    /// <summary>
    /// Construye datos de eventos para notificaciones relacionadas con anteproyectos
    /// </summary>
    public class PreliminaryProjectEventDataBuilder : IPreliminaryProjectEventDataBuilder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentDataService _studentDataService;
        private readonly ILogger<PreliminaryProjectEventDataBuilder> _logger;

        public PreliminaryProjectEventDataBuilder(
            IUnitOfWork unitOfWork,
            IStudentDataService studentDataService,
            ILogger<PreliminaryProjectEventDataBuilder> logger)
        {
            _unitOfWork = unitOfWork;
            _studentDataService = studentDataService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> BuildPreliminaryProjectEventDataAsync(int preliminaryProjectId, string eventType)
        {
            _logger.LogInformation("Building preliminary project event data for PreliminaryProject ID: {PreliminaryProjectId}, Event: {EventType}", 
                preliminaryProjectId, eventType);

            try
            {
                // Obtener el PreliminaryProject
                var preliminaryProjectRepo = _unitOfWork.GetRepository<PreliminaryProject, int>();
                var preliminaryProject = await preliminaryProjectRepo.GetByIdAsync(preliminaryProjectId);
                
                if (preliminaryProject == null)
                {
                    _logger.LogWarning("PreliminaryProject not found with ID: {PreliminaryProjectId}", preliminaryProjectId);
                    return new Dictionary<string, object>();
                }

                // Obtener la InscriptionModality (el ID es el mismo)
                var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
                var inscriptionModality = await inscriptionModalityRepo.GetByIdAsync(preliminaryProjectId);
                
                if (inscriptionModality == null)
                {
                    _logger.LogWarning("InscriptionModality not found with ID: {InscriptionModalityId}", preliminaryProjectId);
                    return new Dictionary<string, object>();
                }

                // Verificar que sea modalidad PROYECTO_GRADO
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscriptionModality.IdModality);
                
                if (modality?.Code != "PROYECTO_GRADO")
                {
                    _logger.LogInformation("PreliminaryProject is not for PROYECTO_GRADO modality (Modality: {Modality}). Skipping notification.", 
                        modality?.Code);
                    return new Dictionary<string, object>();
                }

                // Obtener datos del estudiante
                var (studentNames, studentEmails, studentCount) = 
                    await _studentDataService.GetStudentDataByInscriptionAsync(inscriptionModality.Id);

                // Obtener información del proyecto
                var projectTitle = await GetProjectTitleAsync(inscriptionModality);
                
                // Obtener información del estado actual
                var stateInfo = await GetStateInfoAsync(preliminaryProject.IdStateStage);

                var eventData = new Dictionary<string, object>
                {
                    ["PreliminaryProjectId"] = preliminaryProject.Id,
                    ["InscriptionModalityId"] = inscriptionModality.Id,
                    ["ProjectTitle"] = projectTitle,
                    ["StudentNames"] = studentNames,
                    ["StudentEmails"] = studentEmails,
                    ["StudentsCount"] = studentCount,
                    ["CurrentState"] = stateInfo.StateName,
                    ["CurrentStateCode"] = stateInfo.StateCode,
                    ["SubmissionDate"] = preliminaryProject.UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? 
                                       preliminaryProject.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    ["EventType"] = eventType,
                    ["Phase"] = "Anteproyecto",
                    
                    // Campos adicionales para templates de notificación
                    ["EvaluationResult"] = GetEvaluationResultText(stateInfo.StateCode),
                    ["Observations"] = preliminaryProject.Observations ?? "Sin observaciones registradas",
                    ["ApprovalDate"] = preliminaryProject.ApprovalDate?.ToString("dd/MM/yyyy") ?? string.Empty
                };

                _logger.LogInformation("Preliminary project event data built successfully for PreliminaryProject ID: {PreliminaryProjectId}", preliminaryProjectId);
                return eventData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building preliminary project event data for PreliminaryProject ID: {PreliminaryProjectId}", preliminaryProjectId);
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

                return proposal?.Title ?? "Anteproyecto sin título";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project title for InscriptionModality ID: {InscriptionModalityId}", 
                    inscriptionModality.Id);
                return "Anteproyecto sin título";
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
            if (Enum.TryParse<StateStageCodeEnum>(stateCode, out var code))
            {
                return code switch
                {
                    StateStageCodeEnum.AP_APROBADO => "APROBADO",
                    StateStageCodeEnum.AP_CON_OBSERVACIONES => "CON OBSERVACIONES",
                    StateStageCodeEnum.AP_EN_EVALUACION => "EN EVALUACIÓN",
                    StateStageCodeEnum.AP_RADICADO_PEND_ASIG_EVAL => "RADICADO - PENDIENTE ASIGNACIÓN",
                    StateStageCodeEnum.AP_PENDIENTE_DOCUMENTO => "PENDIENTE DOCUMENTO",
                    _ => "ESTADO DESCONOCIDO"
                };
            }

            return "ESTADO DESCONOCIDO";
        }
    }
}
