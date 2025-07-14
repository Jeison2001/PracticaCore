using Domain.Common;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de notificaciones usando Gmail SMTP
    /// </summary>
    public class GoogleNotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleNotificationService> _logger;

        public GoogleNotificationService(IConfiguration configuration, ILogger<GoogleNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envía un email usando Gmail SMTP
        /// </summary>
        public async Task<bool> SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateGmailSmtpClient();
                using var message = CreateMailMessage(notification);

                await client.SendMailAsync(message, cancellationToken);
                
                _logger.LogInformation("Email enviado exitosamente a {To} via Gmail SMTP", notification.To);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {To} via Gmail SMTP: {Message}", notification.To, ex.Message);
                return false;
            }
        }

        private SmtpClient CreateGmailSmtpClient()
        {
            var googleSettings = _configuration.GetSection("EmailNotification:GoogleSettings");
            
            var client = new SmtpClient
            {
                Host = googleSettings["Host"] ?? "smtp.gmail.com",
                Port = int.Parse(googleSettings["Port"] ?? "587"),
                EnableSsl = true,
                UseDefaultCredentials = false
            };

            var username = googleSettings["Username"];
            var password = googleSettings["Password"];
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }
            else
            {
                throw new InvalidOperationException("Gmail SMTP credentials no están configuradas correctamente en EmailNotification:GoogleSettings");
            }

            return client;
        }

        private MailMessage CreateMailMessage(EmailNotification notification)
        {
            var fromAddress = notification.From ?? _configuration["EmailNotification:GoogleSettings:DefaultFrom"];
            var fromName = notification.FromName ?? _configuration["EmailNotification:GoogleSettings:DefaultFromName"] ?? "Sistema";

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new InvalidOperationException("No se ha configurado una dirección de remitente para Gmail en EmailNotification:GoogleSettings:DefaultFrom");
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
