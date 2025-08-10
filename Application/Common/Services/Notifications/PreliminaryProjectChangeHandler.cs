using Domain.Entities;
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
        private readonly ILogger<PreliminaryProjectChangeHandler> _logger;

        public PreliminaryProjectChangeHandler(
            IEmailNotificationQueueService queueService,
            IPreliminaryProjectEventDataBuilder eventDataBuilder,
            ILogger<PreliminaryProjectChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _logger = logger;
        }

        public async Task HandleChangeAsync(PreliminaryProject oldEntity, PreliminaryProject newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para cambio en PreliminaryProject ID: {PreliminaryProjectId}", newEntity.Id);

            // Solo procesar si cambió el estado
            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = GetPreliminaryProjectEventName(newEntity.IdStateStage);
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
            var eventName = GetPreliminaryProjectEventName(entity.IdStateStage);
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
        private string GetPreliminaryProjectEventName(int stateStageId)
        {
            // Mapeo basado en los eventos configurados en EmailNotificationConfig
            // IMPORTANTE: Estos IDs deben ser actualizados con los valores reales de la BD
            // Ejecutar script: Tables v2/50_GET_STATESTAGE_IDS_FOR_HANDLERS.sql para obtener IDs correctos
            
            return stateStageId switch
            {
                // TODO: CONFIGURAR CON IDs REALES DE StateStage - Valores de ejemplo:
                // Ejemplo: si AP_RADICADO_PEND_ASIG_EVAL tiene ID=6, cambiar por: 6 => "ANTEPROYECTO_SUBMITTED",
                
                // ⚠️ TEMPORAL - Usar IDs reales de StateStage:
                // [ID_AP_RADICADO_PEND_ASIG_EVAL] => "ANTEPROYECTO_SUBMITTED",
                // [ID_AP_APROBADO] => "ANTEPROYECTO_EVALUATION_RESULT",  
                // [ID_AP_CON_OBSERVACIONES] => "ANTEPROYECTO_EVALUATION_RESULT",
                
                _ => string.Empty // No generar evento para otros estados
            };
            
            // EVENTOS CONFIGURADOS DISPONIBLES:
            // - "ANTEPROYECTO_SUBMITTED": Notifica al comité para asignación de evaluadores
            // - "ANTEPROYECTO_EVALUATION_RESULT": Notifica al estudiante del resultado de evaluación
        }
    }
}
