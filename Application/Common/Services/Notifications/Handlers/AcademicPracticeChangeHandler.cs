using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Services.Notifications.Handlers;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services.Notifications.Handlers
{
    /// <summary>
    /// Handler para cambios en Práctica Académica.
    /// Resuelve eventos por código de estado (StateStage.Code) para evitar dependencia de IDs.
    /// </summary>
    public class AcademicPracticeChangeHandler : IEntityChangeHandler<AcademicPractice, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IAcademicPracticeEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AcademicPracticeChangeHandler> _logger;

        public AcademicPracticeChangeHandler(
            IEmailNotificationQueueService queueService,
            IAcademicPracticeEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<AcademicPracticeChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(AcademicPractice oldEntity, AcademicPractice newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing AcademicPractice change for ID: {Id}", newEntity.Id);

            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = await GetAcademicPracticeEventNameAsync(newEntity.IdStateStage, cancellationToken);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildAcademicPracticeEventDataAsync(newEntity.Id, eventName);
                    if (eventData.Count > 0)
                    {
                        var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                        _logger.LogInformation("Enqueued {EventName} for AcademicPractice ID: {Id}, JobId: {JobId}", eventName, newEntity.Id, jobId);
                    }
                    else
                    {
                        _logger.LogWarning("Event data empty for AcademicPractice ID: {Id}, Event: {EventName}", newEntity.Id, eventName);
                    }
                }
            }
        }

        public async Task HandleCreationAsync(AcademicPractice entity, CancellationToken cancellationToken = default)
        {
            var eventName = await GetAcademicPracticeEventNameAsync(entity.IdStateStage, cancellationToken);
            if (!string.IsNullOrEmpty(eventName))
            {
                var eventData = await _eventDataBuilder.BuildAcademicPracticeEventDataAsync(entity.Id, eventName);
                if (eventData.Count > 0)
                {
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    _logger.LogInformation("Enqueued {EventName} for new AcademicPractice ID: {Id}, JobId: {JobId}", eventName, entity.Id, jobId);
                }
            }
        }

        private async Task<string> GetAcademicPracticeEventNameAsync(int stateStageId, CancellationToken cancellationToken)
        {
            try
            {
                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await stateStageRepo.GetByIdAsync(stateStageId);
                var code = stateStage?.Code;
                if (string.IsNullOrWhiteSpace(code)) return string.Empty;

                if (!Enum.TryParse<StateStageCodeEnum>(code, out var stateCode))
                    return string.Empty;

                return stateCode switch
                {
                    // TEMPLATE GENÉRICO: EN_REVISION (3 estados)
                    StateStageCodeEnum.PA_INSCRIPCION_EN_REVISION => "PRACTICA_EN_REVISION",
                    StateStageCodeEnum.PA_DESARROLLO_EN_REVISION => "PRACTICA_EN_REVISION",  // Corregido: notificar al comité cuando estudiante entrega documentos
                    StateStageCodeEnum.PA_INFORME_FINAL_EN_REVISION => "PRACTICA_EN_REVISION",
                    
                    // TEMPLATE GENÉRICO: OBSERVACIONES (3 estados)
                    StateStageCodeEnum.PA_INSCRIPCION_OBSERVACIONES => "PRACTICA_OBSERVACIONES",
                    StateStageCodeEnum.PA_DESARROLLO_OBSERVACIONES => "PRACTICA_OBSERVACIONES",
                    StateStageCodeEnum.PA_INFORME_FINAL_OBSERVACIONES => "PRACTICA_OBSERVACIONES",
                    
                    // TEMPLATE GENÉRICO: APROBADA (2 estados de fase)
                    StateStageCodeEnum.PA_INSCRIPCION_APROBADA => "PRACTICA_APROBADA",
                    StateStageCodeEnum.PA_DESARROLLO_APROBADA => "PRACTICA_APROBADA",
                    
                    // TEMPLATE GENÉRICO: APROBADO_FINAL (1 estado terminal)
                    StateStageCodeEnum.PA_APROBADO => "PRACTICA_APROBADO_FINAL",
                    
                    // TEMPLATE GENÉRICO: NO_APROBADA (3 estados)
                    StateStageCodeEnum.PA_INSCRIPCION_RECHAZADA => "PRACTICA_NO_APROBADA",
                    StateStageCodeEnum.PA_DESARROLLO_NO_APROBADA => "PRACTICA_NO_APROBADA",
                    StateStageCodeEnum.PA_NO_APROBADO => "PRACTICA_NO_APROBADA",
                    
                    // Estados sin notificación (3 estados)
                    StateStageCodeEnum.PA_INSCRIPCION_PEND_DOC => string.Empty,
                    StateStageCodeEnum.PA_DESARROLLO_PEND_DOC => string.Empty,
                    StateStageCodeEnum.PA_INFORME_FINAL_PEND_DOC => string.Empty,
                    
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving AcademicPractice event name for StateStageId {StateStageId}", stateStageId);
                return string.Empty;
            }
        }
    }
}
