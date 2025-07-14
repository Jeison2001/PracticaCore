using Domain.Common;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones usando Azure/Outlook SMTP
    /// </summary>
    public class AzureNotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureNotificationService> _logger;

        public AzureNotificationService(IConfiguration configuration, ILogger<AzureNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envía un email usando Azure/Outlook SMTP
        /// </summary>
        public async Task<bool> SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateAzureSmtpClient();
                using var message = CreateMailMessage(notification);

                await client.SendMailAsync(message, cancellationToken);
                
                _logger.LogInformation("Email enviado exitosamente a {To} via Azure/Outlook SMTP", notification.To);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {To} via Azure/Outlook SMTP: {Message}", notification.To, ex.Message);
                return false;
            }
        }

        private SmtpClient CreateAzureSmtpClient()
        {
            var azureSettings = _configuration.GetSection("Azure:Smtp");
            
            var client = new SmtpClient
            {
                Host = azureSettings["Host"] ?? "smtp-mail.outlook.com",
                Port = int.Parse(azureSettings["Port"] ?? "587"),
                EnableSsl = true,
                UseDefaultCredentials = false
            };

            var username = azureSettings["Username"] ?? azureSettings["Email"];
            var password = azureSettings["Password"];
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }
            else
            {
                throw new InvalidOperationException("Azure/Outlook SMTP credentials no están configuradas correctamente");
            }

            return client;
        }

        private MailMessage CreateMailMessage(EmailNotification notification)
        {
            var fromAddress = notification.From ?? _configuration["Azure:Smtp:DefaultFrom"] ?? _configuration["Azure:Smtp:Email"];
            var fromName = notification.FromName ?? _configuration["Azure:Smtp:DefaultFromName"] ?? "Sistema";

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new InvalidOperationException("No se ha configurado una dirección de remitente para Azure/Outlook");
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = notification.Subject,
                Body = notification.Body,
                IsBodyHtml = notification.IsHtml,
                Priority = ConvertPriority(notification.Priority)
            };

            // Agregar destinatario principal
            message.To.Add(notification.To);

            // Agregar CC
            foreach (var cc in notification.Cc)
            {
                message.CC.Add(cc);
            }

            // Agregar BCC
            foreach (var bcc in notification.Bcc)
            {
                message.Bcc.Add(bcc);
            }

            // Agregar adjuntos
            foreach (var attachment in notification.Attachments)
            {
                var stream = new MemoryStream(attachment.Content);
                var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                message.Attachments.Add(mailAttachment);
            }

            return message;
        }

        private static MailPriority ConvertPriority(EmailPriority priority)
        {
            return priority switch
            {
                EmailPriority.Low => MailPriority.Low,
                EmailPriority.High => MailPriority.High,
                _ => MailPriority.Normal
            };
        }
    }
}
