using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Services.Notifications.Handlers;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Handlers
{
    public class ProjectPresentationChangeHandler : IEntityChangeHandler<ProjectPresentation, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IProjectPresentationEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectPresentationChangeHandler> _logger;

        public ProjectPresentationChangeHandler(
            IEmailNotificationQueueService queueService,
            IProjectPresentationEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<ProjectPresentationChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(ProjectPresentation oldEntity, ProjectPresentation newEntity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Iniciando procesamiento de notificaciones para cambio en ProjectPresentation ID: {Id}", newEntity.Id);

            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = await GetEventNameAsync(newEntity.IdStateStage, cancellationToken);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildProjectPresentationEventDataAsync(newEntity.Id, eventName);

                    if (eventData.Count > 0)
                    {
                        var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                        _logger.LogInformation("Evento {EventName} encolado para sustentación ID: {Id}, JobId: {JobId}",
                            eventName, newEntity.Id, jobId);
                    }
                    else
                    {
                        _logger.LogInformation("No se encoló notificación para sustentación ID: {Id} - datos vacíos", newEntity.Id);
                    }
                }
            }

            _logger.LogInformation("Procesamiento de notificaciones completado para ProjectPresentation ID: {Id}", newEntity.Id);
        }

        public async Task HandleCreationAsync(ProjectPresentation entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Iniciando procesamiento de notificaciones para nueva ProjectPresentation ID: {Id}", entity.Id);

            var eventName = await GetEventNameAsync(entity.IdStateStage, cancellationToken);
            if (!string.IsNullOrEmpty(eventName))
            {
                var eventData = await _eventDataBuilder.BuildProjectPresentationEventDataAsync(entity.Id, eventName);
                if (eventData.Count > 0)
                {
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    _logger.LogInformation("Evento {EventName} encolado para nueva sustentación ID: {Id}, JobId: {JobId}",
                        eventName, entity.Id, jobId);
                }
            }

            _logger.LogInformation("Procesamiento de notificaciones completado para nueva ProjectPresentation ID: {Id}", entity.Id);
        }

        private async Task<string> GetEventNameAsync(int stateStageId, CancellationToken cancellationToken)
        {
            try
            {
                var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await stateStageRepo.GetByIdAsync(stateStageId);
                var code = stateStage?.Code;

                if (!string.IsNullOrWhiteSpace(code) && Enum.TryParse<StateStageCodeEnum>(code, out var stateCode))
                {
                    return stateCode switch
                    {
                        StateStageCodeEnum.SUST_PROGRAMADA => "SUSTENTACION_SCHEDULED",
                        StateStageCodeEnum.SUST_APROBADA => "SUSTENTACION_EVALUATION_RESULT",
                        StateStageCodeEnum.SUST_NO_APROBADA_REINTENTO => "SUSTENTACION_EVALUATION_RESULT",
                        StateStageCodeEnum.SUST_REPROBADA_FINAL => "SUSTENTACION_EVALUATION_RESULT",
                        _ => string.Empty
                    };
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving ProjectPresentation event name for StateStageId {StateStageId}", stateStageId);
                return string.Empty;
            }
        }
    }
}
