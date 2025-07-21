using Domain.Interfaces.Notifications;
using Domain.Common;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Notifications
{
    public class EmailNotificationQueueService : IEmailNotificationQueueService
    {
        private readonly IEmailNotificationEventService _eventService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailNotificationQueueService> _logger;

        public EmailNotificationQueueService(
            IEmailNotificationEventService eventService,
            INotificationService notificationService,
            ILogger<EmailNotificationQueueService> logger)
        {
            _eventService = eventService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public string EnqueueEventNotification(string eventName, Dictionary<string, object> eventData)
        {
            try
            {
                // Encolar tarea para procesamiento en background
                var jobId = BackgroundJob.Enqueue(() => ProcessEventNotificationAsync(eventName, eventData));
                
                _logger.LogInformation("Evento {EventName} encolado con ID: {JobId}", eventName, jobId);
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al encolar evento de notificación {EventName}", eventName);
                throw;
            }
        }

        public string EnqueueDirectEmail(string to, string subject, string body, bool isHtml = true, 
            string[]? cc = null, string[]? bcc = null)
        {
            try
            {
                // Encolar tarea para envío directo en background
                var jobId = BackgroundJob.Enqueue(() => SendDirectEmailAsync(to, subject, body, isHtml, cc, bcc));
                
                _logger.LogInformation("Email directo encolado para {To} con ID: {JobId}", to, jobId);
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al encolar email directo para {To}", to);
                throw;
            }
        }

        public async Task ProcessEventNotificationAsync(string eventName, Dictionary<string, object> eventData)
        {
            try
            {
                // Procesar evento usando el servicio existente
                await _eventService.ProcessEventAsync(eventName, eventData, CancellationToken.None);
                
                _logger.LogInformation("Evento {EventName} procesado exitosamente en background", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento {EventName} en background", eventName);
                throw; // Hangfire reintentará automáticamente
            }
        }

        public async Task SendDirectEmailAsync(string to, string subject, string body, bool isHtml = true, 
            string[]? cc = null, string[]? bcc = null)
        {
            try
            {
                // Crear notificación usando el servicio existente
                var notification = new EmailNotification
                {
                    To = to,
                    Subject = subject,
                    Body = body,
                    IsHtml = isHtml,
                    Cc = cc?.ToList() ?? new List<string>(),
                    Bcc = bcc?.ToList() ?? new List<string>()
                };

                await _notificationService.SendEmailAsync(notification);
                
                _logger.LogInformation("Email directo enviado exitosamente en background para {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email directo en background para {To}", to);
                throw; // Hangfire reintentará automáticamente
            }
        }
    }
}
