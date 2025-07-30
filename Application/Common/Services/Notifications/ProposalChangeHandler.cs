using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Handler específico para cambios en Proposal.
    /// Single Responsibility: Solo maneja notificaciones de Proposal.
    /// </summary>
    public class ProposalChangeHandler : IEntityChangeHandler<Proposal, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IProposalEventDataBuilder _eventDataBuilder;
        private readonly ILogger<ProposalChangeHandler> _logger;

        public ProposalChangeHandler(
            IEmailNotificationQueueService queueService,
            IProposalEventDataBuilder eventDataBuilder,
            ILogger<ProposalChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _logger = logger;
        }

        public async Task HandleChangeAsync(Proposal oldEntity, Proposal newEntity, CancellationToken cancellationToken = default)
        {
            // Solo procesar si cambió el estado
            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = GetProposalEventName((StateStageEnum)newEntity.IdStateStage);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildProposalEventDataAsync(newEntity.Id, eventName);
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    
                    _logger.LogInformation("Evento {EventName} encolado para propuesta ID: {ProposalId}, JobId: {JobId}", 
                        eventName, newEntity.Id, jobId);
                }
            }
        }

        public async Task HandleCreationAsync(Proposal entity, CancellationToken cancellationToken = default)
        {
            var eventName = GetProposalEventName((StateStageEnum)entity.IdStateStage);
            if (!string.IsNullOrEmpty(eventName))
            {
                // Lógica específica para creación de propuestas si es necesaria
                var eventData = await _eventDataBuilder.BuildProposalEventDataAsync(entity.Id, eventName);
                var jobId = _queueService.EnqueueEventNotification(eventName, eventData);

                _logger.LogInformation("Propuesta creada - Evento {EventName} encolado para ID: {ProposalId}, JobId: {JobId}",
                    eventName, entity.Id, jobId);
            }
        }

        private string GetProposalEventName(StateStageEnum stateStage)
        {
            return stateStage switch
            {
                StateStageEnum.PROP_RADICADA => "PROPOSAL_SUBMITTED",
                StateStageEnum.PROP_PERTINENTE => "PROPOSAL_APPROVED",
                StateStageEnum.PROP_NO_PERTINENTE => "PROPOSAL_REJECTED",
                _ => string.Empty
            };
        }
    }
}
