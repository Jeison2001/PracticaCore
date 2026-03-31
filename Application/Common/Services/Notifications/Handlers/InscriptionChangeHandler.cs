using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Handlers;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services.Notifications.Handlers
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

        public async Task HandleCreationAsync(InscriptionModality entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 Iniciando procesamiento de notificaciones para nueva inscripción ID: {InscriptionId}", entity.Id);

            try
            {
                // Retrieve students linked to the new inscription
                var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                var userInscriptions = await userInscriptionRepo.GetAllAsync(ui => ui.IdInscriptionModality == entity.Id && ui.StatusRegister);
                var studentIds = userInscriptions.Select(ui => ui.IdUser).ToList();

                var eventData = await _eventDataBuilder.BuildBasicInscriptionDataAsync(
                    entity.Id,
                    entity.IdModality,
                    entity.IdAcademicPeriod,
                    studentIds);

                var jobId = _queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);

                _logger.LogInformation("✅ Inscription creation notification enqueued - Inscription ID: {InscriptionId}, JobId: {JobId}", entity.Id, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inscription creation notification for ID {Id}", entity.Id);
                throw;
            }
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
