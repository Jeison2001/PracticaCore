using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
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
    private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProposalChangeHandler> _logger;

        public ProposalChangeHandler(
            IEmailNotificationQueueService queueService,
            IProposalEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<ProposalChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(Proposal oldEntity, Proposal newEntity, CancellationToken cancellationToken = default)
        {
            // Solo procesar si cambió el estado
            if (oldEntity.IdStateStage != newEntity.IdStateStage)
            {
                var eventName = await GetProposalEventNameAsync(newEntity.IdStateStage);
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
            var eventName = await GetProposalEventNameAsync(entity.IdStateStage);
            if (!string.IsNullOrEmpty(eventName))
            {
                // Lógica específica para creación de propuestas si es necesaria
                var eventData = await _eventDataBuilder.BuildProposalEventDataAsync(entity.Id, eventName);
                var jobId = _queueService.EnqueueEventNotification(eventName, eventData);

                _logger.LogInformation("Propuesta creada - Evento {EventName} encolado para ID: {ProposalId}, JobId: {JobId}",
                    eventName, entity.Id, jobId);
            }
        }

        private async Task<string> GetProposalEventNameAsync(int stateStageId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<StateStage, int>();
                var stateStage = await repo.GetByIdAsync(stateStageId);
                var code = stateStage?.Code;

                if (string.IsNullOrWhiteSpace(code)) return string.Empty;

                if (!Enum.TryParse<StateStageCodeEnum>(code, ignoreCase: false, out var stateCode))
                    return string.Empty;

                return stateCode switch
                {
                    StateStageCodeEnum.PROP_RADICADA => "PROPOSAL_SUBMITTED",
                    StateStageCodeEnum.PROP_PERTINENTE => "PROPOSAL_APPROVED",
                    StateStageCodeEnum.PROP_NO_PERTINENTE => "PROPOSAL_REJECTED",
                    _ => string.Empty
                };
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
