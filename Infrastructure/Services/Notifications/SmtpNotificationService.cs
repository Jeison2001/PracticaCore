using Domain.Common.Notifications;
using Domain.Enums;
using Domain.Interfaces.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Infrastructure.Services.Notifications
{
    /// <summary>
    /// Servicio único para notificaciones por email usando MailKit (Reemplazo moderno de SmtpClient)
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
        /// Envía un email usando SMTP via MailKit
        /// </summary>
        public async Task<bool> SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = CreateMimeMessage(notification);
                using var client = new SmtpClient();
                
                var smtpSettings = _configuration.GetSection("EmailNotification:SmtpSettings");
                var host = smtpSettings["Host"] ?? "localhost";
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var useSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];

                // Connect
                // StartTls is generally recommended for port 587
                var socketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(host, port, socketOptions, cancellationToken);

                // Authenticate
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password, cancellationToken);
                }

                // Send
                await client.SendAsync(message, cancellationToken);
                
                // Disconnect
                await client.DisconnectAsync(true, cancellationToken);
                
                _logger.LogInformation("Email enviado exitosamente a {To}", notification.To);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {To}: {Message}", notification.To, ex.Message);
                return false;
            }
        }

        private MimeMessage CreateMimeMessage(EmailNotification notification)
        {
            var smtpSettings = _configuration.GetSection("EmailNotification:SmtpSettings");
            var fromAddress = notification.From ?? smtpSettings["DefaultFrom"] ?? "noreply@example.com";
            var fromName = notification.FromName ?? smtpSettings["DefaultFromName"] ?? "Sistema";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(notification.To));
            message.Subject = notification.Subject;

            foreach (var cc in notification.Cc)
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }
            foreach (var bcc in notification.Bcc)
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            var builder = new BodyBuilder();
            if (notification.IsHtml)
            {
                builder.HtmlBody = notification.Body;
            }
            else
            {
                builder.TextBody = notification.Body;
            }

            foreach (var attachment in notification.Attachments)
            {
                builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            message.Body = builder.ToMessageBody();
            message.Priority = ConvertPriority(notification.Priority);

            return message;
        }

        private static MessagePriority ConvertPriority(EmailPriority priority)
        {
            return priority switch
            {
                EmailPriority.Low => MessagePriority.NonUrgent,
                EmailPriority.High => MessagePriority.Urgent,
                _ => MessagePriority.Normal
            };
        }
    }
}
