using Domain.Entities;
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
        private readonly ILogger<ProjectFinalChangeHandler> _logger;

        public ProjectFinalChangeHandler(
            IEmailNotificationQueueService queueService,
            IProjectFinalEventDataBuilder eventDataBuilder,
            ILogger<ProjectFinalChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _logger = logger;
        }

        public async Task HandleChangeAsync(ProjectFinal oldEntity, ProjectFinal newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para cambio en ProjectFinal ID: {ProjectFinalId}", newEntity.Id);

            // Solo procesar si cambió el estado
            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = GetProjectFinalEventName(newEntity.IdStateStage);
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
            var eventName = GetProjectFinalEventName(entity.IdStateStage);
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
        private string GetProjectFinalEventName(int stateStageId)
        {
            // Mapeo basado en los eventos configurados en EmailNotificationConfig
            // IMPORTANTE: Estos IDs deben ser actualizados con los valores reales de la BD
            // Ejecutar script: Tables v2/50_GET_STATESTAGE_IDS_FOR_HANDLERS.sql para obtener IDs correctos
            
            return stateStageId switch
            {
                // TODO: CONFIGURAR CON IDs REALES DE StateStage - Valores de ejemplo:
                // Ejemplo: si PFINF_RADICADO_EN_EVALUACION tiene ID=12, cambiar por: 12 => "PROYECTO_FINAL_SUBMITTED",
                
                // ⚠️ TEMPORAL - Usar IDs reales de StateStage:
                // [ID_PFINF_RADICADO_EN_EVALUACION] => "PROYECTO_FINAL_SUBMITTED",
                // [ID_PFINF_INFORME_APROBADO] => "PROYECTO_FINAL_EVALUATION_RESULT",
                // [ID_PFINF_INFORME_CON_OBSERVACIONES] => "PROYECTO_FINAL_EVALUATION_RESULT",
                
                _ => string.Empty // No generar evento para otros estados
            };
            
            // EVENTOS CONFIGURADOS DISPONIBLES:
            // - "PROYECTO_FINAL_SUBMITTED": Notifica al comité cuando se radica proyecto final
            // - "PROYECTO_FINAL_EVALUATION_RESULT": Notifica al estudiante del resultado de evaluación
        }
    }
}
