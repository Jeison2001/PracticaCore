using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Application.Common.Helpers
{
    public static class NotificationFormattingHelper
    {
        private const string DefaultFrontendUrl = "https://sigepro-five.vercel.app";
        private const string EmptyObservationsActionableMessage =
            "Consulte el detalle y los soportes de evaluación directamente en la plataforma SIGEPRO.";

        private static readonly TimeZoneInfo ColombiaTimeZone = ResolveColombiaTimeZone();
        private static readonly Regex NewlineRegex = new(@"\r?\n", RegexOptions.Compiled);

        public static string ToColombiaDateTime(DateTimeOffset value) =>
            TimeZoneInfo.ConvertTime(value, ColombiaTimeZone)
                .ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);

        public static string ToColombiaDateTime(DateTimeOffset? value, string emptyFallback = "") =>
            value.HasValue ? ToColombiaDateTime(value.Value) : emptyFallback;

        public static string ToColombiaDateTime(DateTime value) =>
            ToColombiaDateTime(NormalizeToDateTimeOffset(value));

        public static string ToColombiaDateTime(DateTime? value, string emptyFallback = "") =>
            value.HasValue ? ToColombiaDateTime(value.Value) : emptyFallback;

        public static string ToColombiaDate(DateTimeOffset value) =>
            TimeZoneInfo.ConvertTime(value, ColombiaTimeZone)
                .ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        public static string ToColombiaDate(DateTimeOffset? value, string emptyFallback = "") =>
            value.HasValue ? ToColombiaDate(value.Value) : emptyFallback;

        public static string ToColombiaDate(DateTime value) =>
            ToColombiaDate(NormalizeToDateTimeOffset(value));

        public static string ToColombiaDate(DateTime? value, string emptyFallback = "") =>
            value.HasValue ? ToColombiaDate(value.Value) : emptyFallback;

        /// <summary>
        /// Formatea una fecha calendario en Colombia protegiendo registros históricos almacenados a medianoche UTC (00:00:00Z)
        /// para evitar retroceder un día calendario.
        /// </summary>
        public static string ToColombiaCalendarDate(DateTimeOffset? value, string fallback = "No registrada")
        {
            if (!value.HasValue)
                return fallback;

            var utc = value.Value.UtcDateTime;
            if (utc.TimeOfDay == TimeSpan.Zero)
                return utc.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            return ToColombiaDate(value.Value);
        }

        public static string NowColombiaDateTime() => ToColombiaDateTime(DateTimeOffset.UtcNow);

        public static string NowColombiaDate() => ToColombiaDate(DateTimeOffset.UtcNow);

        public static string ResolveAppBaseUrl(IConfiguration? configuration)
        {
            var configuredUrl = configuration?["App:FrontendUrl"];
            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                configuredUrl = configuration?
                    .GetSection("AllowedOrigins")
                    .GetChildren()
                    .Select(c => c.Value)
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                    ?? configuration?["AllowedOrigins"];
            }

            var baseUrl = string.IsNullOrWhiteSpace(configuredUrl)
                ? DefaultFrontendUrl
                : configuredUrl.Trim();

            return baseUrl.TrimEnd('/');
        }

        public static string ResolveLoginUrl(IConfiguration? configuration) =>
            $"{ResolveAppBaseUrl(configuration).TrimEnd('/')}/iniciar-sesion";

        public static string GetTopAccentColor(string? tone) =>
            NormalizeTone(tone) switch
            {
                "approved" => "#006633",
                "warning" => "#D97706",
                "rejected" => "#DC2626",
                _ => "#0F172A"
            };

        public static string BuildObservationsBlockHtml(
            string? rawObservations,
            string tone,
            string title = "Observaciones del Comité")
        {
            var normalizedTone = NormalizeTone(tone);
            var effectiveText = rawObservations;

            if (string.IsNullOrWhiteSpace(effectiveText))
            {
                if (normalizedTone is "warning" or "rejected")
                {
                    effectiveText = EmptyObservationsActionableMessage;
                }
                else
                {
                    return string.Empty;
                }
            }

            var (bg, border, accent, titleColor) = normalizedTone switch
            {
                "approved" => ("#F0FDF4", "#BBF7D0", "#006633", "#004D26"),
                "warning" => ("#FFFBEB", "#FDE68A", "#D97706", "#92400E"),
                "rejected" => ("#FEF2F2", "#FECACA", "#DC2626", "#991B1B"),
                _ => ("#F8FAFC", "#E2E8F0", "#0F172A", "#334155")
            };

            var encoded = WebUtility.HtmlEncode(effectiveText.Trim());
            var safeHtml = NewlineRegex.Replace(encoded, "<br />");
            var safeTitle = WebUtility.HtmlEncode(title);

            return $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin-top:20px;background-color:{bg};border:1px solid {border};border-left:4px solid {accent};border-radius:10px;\"><tr><td style=\"padding:16px 20px;\"><p style=\"margin:0 0 6px 0;color:{titleColor};font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:0.6px;\">{safeTitle}</p><p style=\"margin:0;color:#1F2937;font-size:14px;line-height:1.6;\">{safeHtml}</p></td></tr></table>";
        }

        public static string BuildEvaluationBadgeHtml(string label, string tone)
        {
            var (bg, fg, border) = NormalizeTone(tone) switch
            {
                "approved" => ("#DCFCE7", "#166534", "#86EFAC"),
                "warning" => ("#FEF3C7", "#92400E", "#FCD34D"),
                "rejected" => ("#FEE2E2", "#991B1B", "#FCA5A5"),
                _ => ("#F1F5F9", "#334155", "#CBD5E1")
            };

            return $"<span style=\"display:inline-block;padding:4px 12px;border-radius:999px;font-size:11px;font-weight:700;letter-spacing:0.4px;text-transform:uppercase;background-color:{bg};color:{fg};border:1px solid {border};\">{WebUtility.HtmlEncode(label ?? string.Empty)}</span>";
        }

        public static string BuildBulletedListHtml(IEnumerable<string>? items)
        {
            if (items == null)
                return string.Empty;

            var nonEmpty = items
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => $"<li>{WebUtility.HtmlEncode(i.Trim())}</li>")
                .ToList();

            if (nonEmpty.Count == 0)
                return string.Empty;

            return $"<ul style=\"margin:8px 0 0 0;padding-left:20px;color:#374151;font-size:14px;line-height:1.65;\">{string.Concat(nonEmpty)}</ul>";
        }

        private static string NormalizeTone(string? tone) =>
            (tone ?? "info").Trim().ToLowerInvariant();

        private static DateTimeOffset NormalizeToDateTimeOffset(DateTime value) =>
            value.Kind == DateTimeKind.Local
                ? new DateTimeOffset(value)
                : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);

        private static TimeZoneInfo ResolveColombiaTimeZone()
        {
            foreach (var tzId in new[] { "America/Bogota", "SA Pacific Standard Time" })
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(tzId, out var tz))
                    return tz;
            }

            return TimeZoneInfo.CreateCustomTimeZone(
                "COT",
                TimeSpan.FromHours(-5),
                "Colombia Standard Time",
                "Colombia Standard Time");
        }
    }
}
