using System.Net;
using System.Text.RegularExpressions;
using Application.Common.Helpers;
using Domain.Common.Notifications;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Corazón del sistema de notificaciones email. Carga EmailNotificationConfig por evento,
    /// resuelve destinatarios via EmailRecipientResolverService, procesa templates con
    /// placeholders {Key} en una sola pasada segura y envía via INotificationService.
    /// </summary>
    public class EmailNotificationEventService : IEmailNotificationEventService
    {
        private static readonly Regex PlaceholderRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly Regex NewlineRegex = new(@"\r?\n", RegexOptions.Compiled);

        private static readonly HashSet<string> RawHtmlKeys = new(StringComparer.Ordinal)
        {
            "NextSteps",
            "PossibleCauses",
            "AvailableOptions",
            "ObservationsBlock",
            "EvaluationBadgeHtml",
            "TopAccentColor",
            "SpecificObjectivesHtml"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailRecipientResolverService _recipientResolver;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailNotificationEventService> _logger;
        private readonly IConfiguration? _configuration;

        public EmailNotificationEventService(
            IUnitOfWork unitOfWork,
            IEmailRecipientResolverService recipientResolver,
            INotificationService notificationService,
            ILogger<EmailNotificationEventService> logger,
            IConfiguration? configuration = null)
        {
            _unitOfWork = unitOfWork;
            _recipientResolver = recipientResolver;
            _notificationService = notificationService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task ProcessEventAsync(string eventName, Dictionary<string, object> eventData, object? entityContext = null)
        {
            try
            {
                _logger.LogInformation("Procesando evento de notificación: {EventName}", eventName);

                var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
                var configs = await configRepo.GetAllAsync(
                    filter: c => c.EventName == eventName && c.StatusRegister,
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
                throw;
            }
        }

        public async Task<bool> HasActiveConfigurationAsync(string eventName)
        {
            var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
            var config = await configRepo.GetFirstOrDefaultAsync(
                c => c.EventName == eventName && c.StatusRegister,
                CancellationToken.None
            );
            return config != null;
        }

        private async Task ProcessConfigurationAsync(EmailNotificationConfig config, Dictionary<string, object> eventData, object? entityContext)
        {
            try
            {
                var ruleRepo = _unitOfWork.GetRepository<EmailRecipientRule, int>();
                var rules = await ruleRepo.GetAllAsync(
                    filter: r => r.EmailNotificationConfigId == config.Id && r.StatusRegister,
                    orderBy: q => q.OrderBy(r => r.Priority)
                );

                if (!rules.Any())
                {
                    _logger.LogWarning("No hay reglas de destinatarios para la configuración: {ConfigId}", config.Id);
                    return;
                }

                var renderData = eventData != null
                    ? new Dictionary<string, object>(eventData)
                    : new Dictionary<string, object>();

                if (!renderData.TryGetValue("AppUrl", out var existingAppUrl) || string.IsNullOrWhiteSpace(existingAppUrl?.ToString()))
                {
                    renderData["AppUrl"] = NotificationFormattingHelper.ResolveAppBaseUrl(_configuration);
                }

                if (!renderData.TryGetValue("LoginUrl", out var existingLoginUrl) || string.IsNullOrWhiteSpace(existingLoginUrl?.ToString()))
                {
                    renderData["LoginUrl"] = NotificationFormattingHelper.ResolveLoginUrl(_configuration);
                }

                var recipients = await _recipientResolver.ResolveRecipientsAsync(rules.ToList(), renderData, entityContext);

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var toList = (recipients.To ?? [])
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Where(seen.Add)
                    .ToList();

                if (toList.Count == 0)
                {
                    _logger.LogWarning("No se encontraron destinatarios para la configuración: {ConfigId}", config.Id);
                    return;
                }

                var ccList = (recipients.Cc ?? [])
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Where(seen.Add)
                    .ToList();

                var bccList = (recipients.Bcc ?? [])
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Where(seen.Add)
                    .ToList();

                var subject = ProcessSubjectTemplate(config.SubjectTemplate, renderData, config.EventName, config.Id);
                var body = ProcessBodyTemplate(config.BodyTemplate, renderData, config.EventName, config.Id);

                var notification = new EmailNotification
                {
                    To = toList[0],
                    ToAddresses = toList,
                    Subject = subject,
                    Body = body,
                    IsHtml = true,
                    Cc = ccList,
                    Bcc = bccList
                };

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
                    throw new InvalidOperationException(
                        $"No se pudo enviar el email para el evento '{config.EventName}' (ConfigId={config.Id}).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando configuración {ConfigId} para evento {EventName}",
                    config.Id, config.EventName);
                throw;
            }
        }

        private string ProcessSubjectTemplate(
            string template,
            Dictionary<string, object> eventData,
            string eventName,
            int configId)
        {
            return PlaceholderRegex.Replace(template ?? string.Empty, match =>
            {
                var key = match.Groups[1].Value;
                if (!eventData.TryGetValue(key, out var rawObj))
                {
                    _logger.LogWarning(
                        "Placeholder '{{{Key}}}' no encontrado en eventData al renderizar SubjectTemplate de {EventName} (ConfigId={ConfigId}).",
                        key, eventName, configId);
                    return string.Empty;
                }

                var raw = rawObj?.ToString() ?? string.Empty;
                return raw.Replace("\r", " ").Replace("\n", " ").Trim();
            });
        }

        private string ProcessBodyTemplate(
            string template,
            Dictionary<string, object> eventData,
            string eventName,
            int configId)
        {
            return PlaceholderRegex.Replace(template ?? string.Empty, match =>
            {
                var key = match.Groups[1].Value;
                if (!eventData.TryGetValue(key, out var rawObj))
                {
                    _logger.LogWarning(
                        "Placeholder '{{{Key}}}' no encontrado en eventData al renderizar BodyTemplate de {EventName} (ConfigId={ConfigId}).",
                        key, eventName, configId);
                    return string.Empty;
                }

                var raw = rawObj?.ToString() ?? string.Empty;
                if (RawHtmlKeys.Contains(key))
                {
                    return raw;
                }

                var encoded = WebUtility.HtmlEncode(raw);
                return NewlineRegex.Replace(encoded, "<br />");
            });
        }
    }
}
