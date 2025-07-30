using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Handler específico para cambios en InscriptionModality.
    /// Single Responsibility: Solo maneja notificaciones de InscriptionModality.
    /// </summary>
    public class InscriptionChangeHandler : IEntityChangeHandler<InscriptionModality, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IInscriptionEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InscriptionChangeHandler> _logger;

        public InscriptionChangeHandler(
            IEmailNotificationQueueService queueService,
            IInscriptionEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<InscriptionChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(InscriptionModality oldEntity, InscriptionModality newEntity, CancellationToken cancellationToken = default)
        {
            // Solo procesar si cambió el estado
            if (oldEntity.IdStateInscription != newEntity.IdStateInscription)
            {
                var eventName = await GetInscriptionEventNameAsync(newEntity.IdStateInscription, cancellationToken);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildInscriptionEventDataAsync(newEntity.Id, eventName);
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    
                    _logger.LogInformation("Evento {EventName} encolado para inscripción ID: {InscriptionId}, JobId: {JobId}", 
                        eventName, newEntity.Id, jobId);
                }
            }
        }

        public Task HandleCreationAsync(InscriptionModality entity, CancellationToken cancellationToken = default)
        {
            // Esta lógica NO se ejecutará porque eliminamos la llamada del CreateEntityCommandHandler
            // Solo se maneja via CreateInscriptionWithStudentsHandler usando InscriptionCreationService
            _logger.LogDebug("InscriptionModality creation handling skipped - managed by specific handler");
            return Task.CompletedTask;
        }

        private async Task<string> GetInscriptionEventNameAsync(int stateId, CancellationToken cancellationToken)
        {
            try
            {
                var stateRepo = _unitOfWork.GetRepository<StateInscription, int>();
                var state = await stateRepo.GetByIdAsync(stateId);
                
                return state?.Code?.ToUpper() switch
                {
                    "APROBADO" => "INSCRIPTION_APPROVED",
                    "RECHAZADO" => "INSCRIPTION_REJECTED",
                    "PENDIENTE" => "INSCRIPTION_CREATED",
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo nombre de evento para estado ID: {StateId}", stateId);
                return string.Empty;
            }
        }
    }
}
