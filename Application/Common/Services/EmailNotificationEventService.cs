using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services
{
    /// <summary>
    /// Implementación del servicio de eventos de notificación
    /// </summary>
    public class EmailNotificationEventService : IEmailNotificationEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailRecipientResolverService _recipientResolver;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailNotificationEventService> _logger;

        public EmailNotificationEventService(
            IUnitOfWork unitOfWork,
            IEmailRecipientResolverService recipientResolver,
            INotificationService notificationService,
            ILogger<EmailNotificationEventService> logger)
        {
            _unitOfWork = unitOfWork;
            _recipientResolver = recipientResolver;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessEventAsync(string eventName, Dictionary<string, object> eventData, object? entityContext = null)
        {
            try
            {
                _logger.LogInformation("Procesando evento de notificación: {EventName}", eventName);

                // Obtener configuraciones activas para el evento
                var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
                var configs = await configRepo.GetAllAsync(
                    filter: c => c.EventName == eventName && c.IsActive,
                    orderBy: q => q.OrderBy(c => c.Id)
                );

                if (!configs.Any())
                {
                    _logger.LogInformation("No hay configuraciones activas para el evento: {EventName}", eventName);
                    return;
                }

                foreach (var config in configs)
                {
                    await ProcessConfigurationAsync(config, eventData, entityContext);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento de notificación: {EventName}", eventName);
            }
        }

        public async Task<bool> HasActiveConfigurationAsync(string eventName)
        {
            var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
            var config = await configRepo.GetFirstOrDefaultAsync(
                c => c.EventName == eventName && c.IsActive,
                CancellationToken.None
            );
            return config != null;
        }

        private async Task ProcessConfigurationAsync(EmailNotificationConfig config, Dictionary<string, object> eventData, object? entityContext)
        {
            try
            {
                // Cargar reglas de destinatarios
                var ruleRepo = _unitOfWork.GetRepository<EmailRecipientRule, int>();
                var rules = await ruleRepo.GetAllAsync(
                    filter: r => r.EmailNotificationConfigId == config.Id,
                    orderBy: q => q.OrderBy(r => r.Priority)
                );

                if (!rules.Any())
                {
                    _logger.LogWarning("No hay reglas de destinatarios para la configuración: {ConfigId}", config.Id);
                    return;
                }

                // Resolver destinatarios
                var recipients = await _recipientResolver.ResolveRecipientsAsync(rules.ToList(), eventData, entityContext);

                if (!recipients.To.Any())
                {
                    _logger.LogWarning("No se encontraron destinatarios para la configuración: {ConfigId}", config.Id);
                    return;
                }

                // Procesar plantillas
                var subject = ProcessTemplate(config.SubjectTemplate, eventData);
                var body = ProcessTemplate(config.BodyTemplate, eventData);

                // Crear notificación
                var notification = new EmailNotification
                {
                    To = recipients.To.First(), // Primer destinatario principal
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    Cc = recipients.Cc,
                    Bcc = recipients.Bcc
                };

                // Agregar destinatarios adicionales como BCC si hay más de uno
                if (recipients.To.Count > 1)
                {
                    notification.Bcc.AddRange(recipients.To.Skip(1));
                }

                // Enviar email directamente
                var success = await _notificationService.SendEmailAsync(notification);

                if (success)
                {
                    _logger.LogInformation("Email enviado exitosamente para evento {EventName} - Config {ConfigId}", 
                        config.EventName, config.Id);
                }
                else
                {
                    _logger.LogWarning("Error al enviar email para evento {EventName} - Config {ConfigId}", 
                        config.EventName, config.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando configuración {ConfigId} para evento {EventName}", 
                    config.Id, config.EventName);
            }
        }

        private static string ProcessTemplate(string template, Dictionary<string, object> eventData)
        {
            var result = template;
            
            // Reemplazar placeholders del tipo {Key}
            foreach (var kvp in eventData)
            {
                var placeholder = $"{{{kvp.Key}}}";
                var value = kvp.Value?.ToString() ?? string.Empty;
                result = result.Replace(placeholder, value);
            }

            return result;
        }
    }
}
