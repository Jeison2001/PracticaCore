using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Handler específico para cambios en ProjectFinal.
    /// Single Responsibility: Solo maneja notificaciones de proyectos finales.
    /// </summary>
    public class ProjectFinalChangeHandler : IEntityChangeHandler<ProjectFinal, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IProjectFinalEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectFinalChangeHandler> _logger;

        public ProjectFinalChangeHandler(
            IEmailNotificationQueueService queueService,
            IProjectFinalEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<ProjectFinalChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(ProjectFinal oldEntity, ProjectFinal newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para cambio en ProjectFinal ID: {ProjectFinalId}", newEntity.Id);

            // Solo procesar si cambió el estado
            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = await GetProjectFinalEventNameAsync(newEntity.IdStateStage, cancellationToken);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildProjectFinalEventDataAsync(newEntity.Id, eventName);
                    
                    // Solo encolar si se construyeron datos válidos
                    if (eventData.Count > 0)
                    {
                        var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                        _logger.LogInformation("📧 Evento {EventName} encolado para proyecto final ID: {ProjectFinalId}, JobId: {JobId}", 
                            eventName, newEntity.Id, jobId);
                    }
                    else
                    {
                        _logger.LogInformation("⚠️ No se encoló notificación para proyecto final ID: {ProjectFinalId} - datos de evento vacíos", 
                            newEntity.Id);
                    }
                }
                else
                {
                    _logger.LogDebug("No hay evento configurado para el estado {StateStageId} en proyecto final ID: {ProjectFinalId}", 
                        newEntity.IdStateStage, newEntity.Id);
                }
            }

            _logger.LogInformation("✅ Procesamiento de notificaciones completado para ProjectFinal ID: {ProjectFinalId}", newEntity.Id);
        }

        public async Task HandleCreationAsync(ProjectFinal entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para nuevo ProjectFinal ID: {ProjectFinalId}", entity.Id);

            // Para nuevos proyectos finales, considerar notificar según el estado inicial
            var eventName = await GetProjectFinalEventNameAsync(entity.IdStateStage, cancellationToken);
            if (!string.IsNullOrEmpty(eventName))
            {
                var eventData = await _eventDataBuilder.BuildProjectFinalEventDataAsync(entity.Id, eventName);
                
                if (eventData.Count > 0)
                {
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    _logger.LogInformation("📧 Evento {EventName} encolado para nuevo proyecto final ID: {ProjectFinalId}, JobId: {JobId}", 
                        eventName, entity.Id, jobId);
                }
            }

            _logger.LogInformation("✅ Procesamiento de notificaciones completado para nuevo ProjectFinal ID: {ProjectFinalId}", entity.Id);
        }

        /// <summary>
        /// Mapea el estado del proyecto final a un nombre de evento
        /// </summary>
        private async Task<string> GetProjectFinalEventNameAsync(int stateStageId, CancellationToken cancellationToken)
        {
            // Resolver por código del estado para evitar dependencia de IDs entre ambientes
            try
            {
                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await stateStageRepo.GetByIdAsync(stateStageId);
                var code = stateStage?.Code;

                if (!string.IsNullOrWhiteSpace(code) && Enum.TryParse<StateStageCodeEnum>(code, out var stateCode))
                {
                    return stateCode switch
                    {
                        StateStageCodeEnum.PFINF_RADICADO_EN_EVALUACION => "PROYECTO_FINAL_SUBMITTED",
                        StateStageCodeEnum.PFINF_INFORME_APROBADO => "PROYECTO_FINAL_EVALUATION_RESULT",
                        StateStageCodeEnum.PFINF_INFORME_CON_OBSERVACIONES => "PROYECTO_FINAL_EVALUATION_RESULT",
                        _ => string.Empty
                    };
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving ProjectFinal event name for StateStageId {StateStageId}", stateStageId);
                return string.Empty;
            }
        }
    }
}
