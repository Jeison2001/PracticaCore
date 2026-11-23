using System.Text.Json;
using Api.Serialization;
using FluentAssertions;
using Xunit;

namespace Tests.UnitTests.Api.Serialization
{
    public class UtcDateTimeOffsetJsonConverterTests
    {
        private sealed record PayloadDto(DateTimeOffset RequiredDate, DateTimeOffset? OptionalDate);

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new UtcDateTimeOffsetJsonConverter());
            options.Converters.Add(new NullableUtcDateTimeOffsetJsonConverter());
            return options;
        }

        [Fact]
        public void Deserialize_CalendarDate_NormalizesToNoonUtc()
        {
            var options = CreateOptions();
            const string json = """{"requiredDate":"2026-06-15","optionalDate":"2026-06-16"}""";

            var dto = JsonSerializer.Deserialize<PayloadDto>(json, options)!;

            dto.RequiredDate.Should().Be(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
            dto.RequiredDate.Offset.Should().Be(TimeSpan.Zero);
            dto.OptionalDate.Should().Be(new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));
            dto.OptionalDate!.Value.Offset.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void Deserialize_ExplicitOffset_And_NoOffsetDateTime_ConvertToUtc()
        {
            var options = CreateOptions();
            const string json = """
                {
                  "requiredDate": "2026-06-15T22:00:00-05:00",
                  "optionalDate": "2026-06-15T22:00:00"
                }
                """;

            var dto = JsonSerializer.Deserialize<PayloadDto>(json, options)!;

            var expectedUtc = new DateTimeOffset(2026, 6, 16, 3, 0, 0, TimeSpan.Zero);
            dto.RequiredDate.Should().Be(expectedUtc);
            dto.RequiredDate.Offset.Should().Be(TimeSpan.Zero);
            dto.OptionalDate.Should().Be(expectedUtc);
            dto.OptionalDate!.Value.Offset.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void Deserialize_NullableNullOrWhitespace_ReturnsNull()
        {
            var options = CreateOptions();

            var withNull = JsonSerializer.Deserialize<PayloadDto>("""{"requiredDate":"2026-06-15","optionalDate":null}""", options)!;
            var withWhitespace = JsonSerializer.Deserialize<PayloadDto>("""{"requiredDate":"2026-06-15","optionalDate":"   "}""", options)!;

            withNull.OptionalDate.Should().BeNull();
            withWhitespace.OptionalDate.Should().BeNull();
        }

        [Fact]
        public void Serialize_NonUtcDateTimeOffset_WritesUtcIso8601WithZ()
        {
            var options = CreateOptions();
            var dto = new PayloadDto(
                new DateTimeOffset(2026, 6, 15, 22, 0, 0, TimeSpan.FromHours(-5)),
                null);

            var json = JsonSerializer.Serialize(dto, options);

            json.Should().Contain("\"requiredDate\":\"2026-06-16T03:00:00.0000000Z\"");
            json.Should().Contain("\"optionalDate\":null");
        }
    }
}
