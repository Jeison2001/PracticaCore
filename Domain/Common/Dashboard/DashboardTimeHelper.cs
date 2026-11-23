using System.Globalization;

namespace Domain.Common.Dashboard
{
    /// <summary>
    /// Utilidades de zona horaria de Colombia (America/Bogota, UTC-5) para consultas
    /// inclusivas por rango de fechas y formateo consistente de reportes.
    /// </summary>
    public static class DashboardTimeHelper
    {
        public static readonly TimeZoneInfo ColombiaTimeZone = ResolveColombiaTimeZone();

        /// <summary>
        /// Convierte la fecha calendario recibida al inicio del día (00:00:00) en hora Colombia, expresado en UTC.
        /// </summary>
        public static DateTimeOffset ToColombiaDayStartUtc(DateTimeOffset input)
        {
            var date = ExtractColombiaCalendarDate(input);
            var localMidnight = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var offset = ColombiaTimeZone.GetUtcOffset(localMidnight);
            return new DateTimeOffset(localMidnight, offset).ToUniversalTime();
        }

        /// <summary>
        /// Convierte la fecha calendario recibida al inicio del día siguiente (00:00:00 del día+1) en hora Colombia,
        /// expresado en UTC, para usar límite superior exclusivo (&lt; nextDayStartUtc) que incluye todo el día.
        /// </summary>
        public static DateTimeOffset ToColombiaNextDayStartUtc(DateTimeOffset input)
        {
            var date = ExtractColombiaCalendarDate(input).AddDays(1);
            var localMidnight = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var offset = ColombiaTimeZone.GetUtcOffset(localMidnight);
            return new DateTimeOffset(localMidnight, offset).ToUniversalTime();
        }

        public static string FormatDateTime(DateTimeOffset value) =>
            TimeZoneInfo.ConvertTime(value, ColombiaTimeZone)
                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public static string FormatDateTime(DateTimeOffset? value) =>
            value.HasValue ? FormatDateTime(value.Value) : "";

        public static string FormatDate(DateTimeOffset? value) =>
            value.HasValue
                ? TimeZoneInfo.ConvertTime(value.Value, ColombiaTimeZone)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : "";

        public static DateTimeOffset NowColombia() =>
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ColombiaTimeZone);

        private static DateOnly ExtractColombiaCalendarDate(DateTimeOffset input)
        {
            // Cuando el cliente envía una fecha calendario sin hora (p.ej. 2026-03-15T00:00:00Z o -05:00),
            // respetamos directamente esa fecha calendario para evitar retroceder un día al convertir desde UTC.
            if (input.TimeOfDay == TimeSpan.Zero)
                return DateOnly.FromDateTime(input.Date);

            var colombia = TimeZoneInfo.ConvertTime(input, ColombiaTimeZone);
            return DateOnly.FromDateTime(colombia.Date);
        }

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
