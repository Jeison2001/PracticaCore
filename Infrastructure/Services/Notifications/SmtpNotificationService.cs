using Domain.Common;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services.Notifications
{
    /// <summary>
    /// Servicio único para notificaciones por email usando cualquier proveedor SMTP (Gmail, Outlook, etc.)
    /// </summary>
    public class SmtpNotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpNotificationService> _logger;

        public SmtpNotificationService(IConfiguration configuration, ILogger<SmtpNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envía un email usando SMTP
        /// </summary>
        public async Task<bool> SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var message = CreateMailMessage(notification);

                await client.SendMailAsync(message, cancellationToken);
                
                _logger.LogInformation("Email enviado exitosamente a {To}", notification.To);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {To}: {Message}", notification.To, ex.Message);
                return false;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            var smtpSettings = _configuration.GetSection("EmailNotification:SmtpSettings");
            var client = new SmtpClient
            {
                Host = smtpSettings["Host"] ?? "localhost",
                Port = int.Parse(smtpSettings["Port"] ?? "587"),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true"),
                UseDefaultCredentials = false
            };
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }
            return client;
        }

        private MailMessage CreateMailMessage(EmailNotification notification)
        {
            var smtpSettings = _configuration.GetSection("EmailNotification:SmtpSettings");
            var fromAddress = notification.From ?? smtpSettings["DefaultFrom"] ?? "noreply@example.com";
            var fromName = notification.FromName ?? smtpSettings["DefaultFromName"] ?? "Sistema";
            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = notification.Subject,
                Body = notification.Body,
                IsBodyHtml = notification.IsHtml,
                Priority = ConvertPriority(notification.Priority)
            };
            message.To.Add(notification.To);
            foreach (var cc in notification.Cc)
            {
                message.CC.Add(cc);
            }
            foreach (var bcc in notification.Bcc)
            {
                message.Bcc.Add(bcc);
            }
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
