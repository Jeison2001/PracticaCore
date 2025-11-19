using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Shared.DTOs.InscriptionWithStudents;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.Inscription
{
    public class InscriptionIntegrationTests : IntegrationTestBase
    {
        public InscriptionIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateInscription_WithEmptyStudentsList_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto { IdModality = 1 },
                Students = new List<UserInscriptionModalityCreateDto>() // Empty list
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", invalidDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            var jsonDoc = JsonDocument.Parse(content);
            var errors = jsonDoc.RootElement.GetProperty("Errors").EnumerateArray()
                .Select(e => e.GetString()).ToList();
            
            errors.Should().Contain(e => e.Contains("estudiante"));
        }

        [Fact]
        public async Task CreateInscription_WithInvalidStudentData_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto { IdModality = 1 },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto
                    {
                        Identification = "", // Invalid: Empty identification
                        IdIdentificationType = 0 // Invalid: 0
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", invalidDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            var jsonDoc = JsonDocument.Parse(content);
            var errors = jsonDoc.RootElement.GetProperty("Errors").EnumerateArray()
                .Select(e => e.GetString()).ToList();
            
            errors.Should().Contain(e => e.Contains("identificación"));
        }
    }
}
