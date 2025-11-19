using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Shared.DTOs.AcademicPractice;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.AcademicPractice
{
    public class AcademicPracticeIntegrationTests : IntegrationTestBase
    {
        public AcademicPracticeIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task UpdateInstitutionInfo_WithMissingTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDto = new UpdateInstitutionInfoDto
            {
                Id = 1,
                // Missing Title - should trigger validation error
                InstitutionName = "Test Institution",
                InstitutionContact = "Contact",
                PracticeStartDate = DateTime.Now,
                PracticeEndDate = DateTime.Now.AddDays(1),
                PracticeHours = 100
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/AcademicPractice/UpdateInstitutionInfo/1", invalidDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            // Parse JSON to verify error structure
            var jsonDoc = JsonDocument.Parse(content);
            jsonDoc.RootElement.GetProperty("Success").GetBoolean().Should().BeFalse();
            
            var errors = jsonDoc.RootElement.GetProperty("Errors").EnumerateArray()
                .Select(e => e.GetString()).ToList();
            
            errors.Should().Contain(e => e.Contains("título") && e.Contains("requerido"));
        }

        [Fact]
        public async Task UpdateInstitutionInfo_WithInvalidDates_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDto = new UpdateInstitutionInfoDto
            {
                Id = 1,
                Title = "Test Title",
                InstitutionName = "Test Institution",
                InstitutionContact = "Contact",
                PracticeStartDate = DateTime.Now.AddDays(10),
                PracticeEndDate = DateTime.Now, // End date before start date
                PracticeHours = 100
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/AcademicPractice/UpdateInstitutionInfo/1", invalidDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            var jsonDoc = JsonDocument.Parse(content);
            var errors = jsonDoc.RootElement.GetProperty("Errors").EnumerateArray()
                .Select(e => e.GetString()).ToList();
            
            errors.Should().Contain(e => e.Contains("fecha") && e.Contains("posterior"));
        }
    }
}
