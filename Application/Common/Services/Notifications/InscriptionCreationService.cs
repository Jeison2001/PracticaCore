using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Servicio específico para manejar la creación de inscripciones con estudiantes.
    /// Reemplaza la lógica manual del CreateInscriptionWithStudentsHandler.
    /// </summary>
    public class InscriptionCreationService : IInscriptionCreationService
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IInscriptionEventDataBuilder _eventDataBuilder;
        private readonly ILogger<InscriptionCreationService> _logger;

        public InscriptionCreationService(
            IEmailNotificationQueueService queueService,
            IInscriptionEventDataBuilder eventDataBuilder,
            ILogger<InscriptionCreationService> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _logger = logger;
        }

        public async Task ProcessInscriptionCreationAsync(int inscriptionId, int modalityId, int academicPeriodId, IEnumerable<int> studentIds, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing inscription creation notification for Inscription ID: {InscriptionId}", inscriptionId);

                // Construir datos del evento con la información disponible
                var eventData = await _eventDataBuilder.BuildBasicInscriptionDataAsync(
                    inscriptionId, 
                    modalityId, 
                    academicPeriodId, 
                    studentIds);

                // Encolar notificación
                var jobId = _queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);
                
                _logger.LogInformation("Inscription creation notification enqueued - Inscription ID: {InscriptionId}, Students: {StudentCount}, JobId: {JobId}",
                    inscriptionId, eventData["StudentCount"], jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inscription creation notification for Inscription ID: {InscriptionId}", inscriptionId);
                // No re-throw para evitar que falle la operación principal de creación
            }
        }
    }
}
