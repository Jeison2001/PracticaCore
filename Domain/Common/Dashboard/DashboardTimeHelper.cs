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

        /// <summary>
        /// Cuenta los días hábiles (lunes a viernes) transcurridos en el intervalo (startUtc, endUtc]
        /// evaluando las fechas calendario en zona horaria de Colombia (America/Bogota, UTC-5).
        /// </summary>
        public static int CountBusinessDaysElapsed(DateTimeOffset startUtc, DateTimeOffset endUtc)
        {
            var startColombia = TimeZoneInfo.ConvertTime(startUtc, ColombiaTimeZone);
            var endColombia = TimeZoneInfo.ConvertTime(endUtc, ColombiaTimeZone);

            var startDate = DateOnly.FromDateTime(startColombia.Date);
            var endDate = DateOnly.FromDateTime(endColombia.Date);

            if (endDate <= startDate)
                return 0;

            var totalDays = endDate.DayNumber - startDate.DayNumber;
            var fullWeeks = totalDays / 7;
            var remainder = totalDays % 7;
            var businessDays = fullWeeks * 5;

            var cursor = startDate.AddDays(fullWeeks * 7);
            for (var i = 1; i <= remainder; i++)
            {
                var dow = cursor.AddDays(i).DayOfWeek;
                if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                    businessDays++;
            }

            return businessDays;
        }

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
