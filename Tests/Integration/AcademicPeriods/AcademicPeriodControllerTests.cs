using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Responses;
using Application.Shared.DTOs.AcademicPeriods;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.AcademicPeriods
{
    public class AcademicPeriodControllerTests : GenericControllerIntegrationTests<AcademicPeriod, AcademicPeriodDto>
    {
        public AcademicPeriodControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/AcademicPeriod";

        protected override AcademicPeriodDto CreateValidDto()
        {
            return new AcademicPeriodDto
            {
                Code = $"PER_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
        }

        protected override AcademicPeriod CreateValidEntity()
        {
            return new AcademicPeriod
            {
                Code = $"PER_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
        }

        [Fact]
        public async Task CreateMultiple_WithInvalidEndDate_ReturnsBadRequest_AndValidListReturnsOk()
        {
            var invalidList = new List<AcademicPeriodDto>
            {
                new()
                {
                    Code = $"BAD_{Guid.NewGuid().ToString("N")[..6]}",
                    StartDate = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
                    EndDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
                    StatusRegister = true
                }
            };

            var badResponse = await _client.PostAsJsonAsync($"{BaseUrl}/multiple", invalidList);
            badResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var badContent = await badResponse.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(badContent))
            {
                doc.RootElement.TryGetProperty("errors", out var errs).Should().BeTrue();
                errs.GetArrayLength().Should().BeGreaterThan(0);
            }

            var validList = new List<AcademicPeriodDto> { CreateValidDto(), CreateValidDto() };
            var okResponse = await _client.PostAsJsonAsync($"{BaseUrl}/multiple", validList);
            okResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var okPayload = await okResponse.Content.ReadFromJsonAsync<ApiResponse<List<AcademicPeriodDto>>>();
            okPayload!.Success.Should().BeTrue();
            okPayload.Data!.Should().HaveCount(2);
        }
    }
}