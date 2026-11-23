using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Domain.Common.Dashboard;

namespace Api.Serialization
{
    /// <summary>
    /// Estandariza la deserialización y serialización JSON de <see cref="DateTimeOffset"/> en UTC ISO-8601.
    /// - Fechas calendario puras ("yyyy-MM-dd") se normalizan a 12:00:00Z para evitar desfases de ±1 día.
    /// - Fechas con offset explícito se convierten a UTC (.ToUniversalTime()).
    /// - Fechas con hora sin offset se interpretan en hora Colombia (America/Bogota) y se convierten a UTC.
    /// </summary>
    public sealed class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
    {
        private static readonly Regex CalendarDateRegex = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
        private static readonly Regex ExplicitOffsetRegex = new(@"(?:[Zz]|[+-]\d{2}:?\d{2})$", RegexOptions.Compiled);

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Se esperaba una cadena de fecha ISO-8601 válida para DateTimeOffset.");
            }

            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new JsonException("No se admite una cadena vacía para un campo DateTimeOffset obligatorio.");
            }

            return ParseToUtc(raw);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(FormatUtcIso(value));
        }

        internal static DateTimeOffset ParseToUtc(string raw)
        {
            var trimmed = raw.Trim();

            if (CalendarDateRegex.IsMatch(trimmed))
            {
                if (DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var calendarDate))
                {
                    return new DateTimeOffset(calendarDate.Year, calendarDate.Month, calendarDate.Day, 12, 0, 0, TimeSpan.Zero);
                }

                throw new JsonException($"Fecha calendario inválida: '{raw}'.");
            }

            if (ExplicitOffsetRegex.IsMatch(trimmed))
            {
                if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    return dto.ToUniversalTime();
                }

                throw new JsonException($"Formato de fecha con offset inválido: '{raw}'.");
            }

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                var unspecified = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
                var offset = DashboardTimeHelper.ColombiaTimeZone.GetUtcOffset(unspecified);
                return new DateTimeOffset(unspecified, offset).ToUniversalTime();
            }

            throw new JsonException($"Formato de fecha inválido: '{raw}'.");
        }

        internal static string FormatUtcIso(DateTimeOffset value) =>
            value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Variante nullable de <see cref="UtcDateTimeOffsetJsonConverter"/>.
    /// </summary>
    public sealed class NullableUtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Se esperaba una cadena de fecha ISO-8601 válida o null para DateTimeOffset?.");
            }

            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return UtcDateTimeOffsetJsonConverter.ParseToUtc(raw);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(UtcDateTimeOffsetJsonConverter.FormatUtcIso(value.Value));
        }
    }
}
