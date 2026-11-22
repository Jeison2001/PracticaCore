using System.Net;
using System.Text.RegularExpressions;
using Domain.Common.Notifications;
using Domain.Enums;
using Domain.Interfaces.Services.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Services.Notifications
{
    /// <summary>
    /// Servicio único para notificaciones por email usando MailKit (Reemplazo moderno de SmtpClient)
    /// </summary>
    public class SmtpNotificationService : INotificationService
    {
        private const string DefaultSenderName = "SIGEPRO · Universidad Popular del Cesar";

        private static readonly Regex StyleOrHeadBlockRegex = new(
            @"<(style|head)\b[^>]*>[\s\S]*?</\1>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LineBreakTagRegex = new(
            @"<br\s*/?>|</(p|tr|li|h[1-6])>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AnyHtmlTagRegex = new(
            @"<[^>]+>",
            RegexOptions.Compiled);
        private static readonly Regex HorizontalSpacesRegex = new(
            @"[ \t]+",
            RegexOptions.Compiled);
        private static readonly Regex ExcessiveNewlinesRegex = new(
            @"(\r?\n[ \t]*){3,}",
            RegexOptions.Compiled);

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

                var socketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(host, port, socketOptions, cancellationToken);

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email enviado exitosamente a {To}", string.Join(", ", message.To));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {To}: {Message}", notification.To, ex.Message);
                return false;
            }
        }

        public MimeMessage CreateMimeMessage(EmailNotification notification)
        {
            var smtpSettings = _configuration.GetSection("EmailNotification:SmtpSettings");
            var fromAddress = !string.IsNullOrWhiteSpace(notification.From)
                ? notification.From
                : smtpSettings["DefaultFrom"] ?? "noreply@example.com";
            var fromName = !string.IsNullOrWhiteSpace(notification.FromName)
                ? notification.FromName
                : (!string.IsNullOrWhiteSpace(smtpSettings["DefaultFromName"])
                    ? smtpSettings["DefaultFromName"]!
                    : DefaultSenderName);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));

            var toAddresses = notification.ToAddresses is { Count: > 0 }
                ? notification.ToAddresses
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : (!string.IsNullOrWhiteSpace(notification.To) ? [notification.To.Trim()] : []);

            foreach (var to in toAddresses)
            {
                message.To.Add(MailboxAddress.Parse(to));
            }

            message.Subject = notification.Subject;

            foreach (var cc in notification.Cc.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }

            foreach (var bcc in notification.Bcc.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            var builder = new BodyBuilder();
            if (notification.IsHtml)
            {
                builder.HtmlBody = notification.Body;
                builder.TextBody = ConvertHtmlToPlainText(notification.Body);
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

        internal static string ConvertHtmlToPlainText(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var cleaned = StyleOrHeadBlockRegex.Replace(html, string.Empty);
            cleaned = LineBreakTagRegex.Replace(cleaned, "\n");
            cleaned = AnyHtmlTagRegex.Replace(cleaned, string.Empty);
            cleaned = WebUtility.HtmlDecode(cleaned);
            cleaned = HorizontalSpacesRegex.Replace(cleaned, " ");

            var lines = cleaned
                .Split('\n')
                .Select(l => l.Trim());

            var joined = string.Join("\n", lines);
            return ExcessiveNewlinesRegex.Replace(joined, "\n\n").Trim();
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
