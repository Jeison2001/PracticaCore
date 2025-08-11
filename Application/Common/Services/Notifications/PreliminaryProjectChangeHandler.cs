using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Handler específico para cambios en PreliminaryProject.
    /// Single Responsibility: Solo maneja notificaciones de anteproyectos.
    /// </summary>
    public class PreliminaryProjectChangeHandler : IEntityChangeHandler<PreliminaryProject, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IPreliminaryProjectEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PreliminaryProjectChangeHandler> _logger;

        public PreliminaryProjectChangeHandler(
            IEmailNotificationQueueService queueService,
            IPreliminaryProjectEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<PreliminaryProjectChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(PreliminaryProject oldEntity, PreliminaryProject newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para cambio en PreliminaryProject ID: {PreliminaryProjectId}", newEntity.Id);

            // Solo procesar si cambió el estado
            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = await GetPreliminaryProjectEventNameAsync(newEntity.IdStateStage, cancellationToken);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildPreliminaryProjectEventDataAsync(newEntity.Id, eventName);
                    
                    // Solo encolar si se construyeron datos válidos
                    if (eventData.Count > 0)
                    {
                        var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                        _logger.LogInformation("📧 Evento {EventName} encolado para anteproyecto ID: {PreliminaryProjectId}, JobId: {JobId}", 
                            eventName, newEntity.Id, jobId);
                    }
                    else
                    {
                        _logger.LogInformation("⚠️ No se encoló notificación para anteproyecto ID: {PreliminaryProjectId} - datos de evento vacíos", 
                            newEntity.Id);
                    }
                }
                else
                {
                    _logger.LogDebug("No hay evento configurado para el estado {StateStageId} en anteproyecto ID: {PreliminaryProjectId}", 
                        newEntity.IdStateStage, newEntity.Id);
                }
            }

            _logger.LogInformation("✅ Procesamiento de notificaciones completado para PreliminaryProject ID: {PreliminaryProjectId}", newEntity.Id);
        }

        public async Task HandleCreationAsync(PreliminaryProject entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para nuevo PreliminaryProject ID: {PreliminaryProjectId}", entity.Id);

            // Para nuevos anteproyectos, considerar notificar según el estado inicial
            var eventName = await GetPreliminaryProjectEventNameAsync(entity.IdStateStage, cancellationToken);
            if (!string.IsNullOrEmpty(eventName))
            {
                var eventData = await _eventDataBuilder.BuildPreliminaryProjectEventDataAsync(entity.Id, eventName);
                
                if (eventData.Count > 0)
                {
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    _logger.LogInformation("📧 Evento {EventName} encolado para nuevo anteproyecto ID: {PreliminaryProjectId}, JobId: {JobId}", 
                        eventName, entity.Id, jobId);
                }
            }

            _logger.LogInformation("✅ Procesamiento de notificaciones completado para nuevo PreliminaryProject ID: {PreliminaryProjectId}", entity.Id);
        }

        /// <summary>
        /// Mapea el estado del anteproyecto a un nombre de evento
        /// </summary>
        private async Task<string> GetPreliminaryProjectEventNameAsync(int stateStageId, CancellationToken cancellationToken)
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
                        StateStageCodeEnum.AP_RADICADO_PEND_ASIG_EVAL => "ANTEPROYECTO_SUBMITTED",
                        StateStageCodeEnum.AP_APROBADO => "ANTEPROYECTO_EVALUATION_RESULT",
                        StateStageCodeEnum.AP_CON_OBSERVACIONES => "ANTEPROYECTO_EVALUATION_RESULT",
                        _ => string.Empty
                    };
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving PreliminaryProject event name for StateStageId {StateStageId}", stateStageId);
                return string.Empty;
            }
        }
    }
}
