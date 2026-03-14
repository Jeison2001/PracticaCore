using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using FluentAssertions;
using Xunit;
using System.Text.Json;

namespace Tests.Integration.Utilities
{
    public class ServicesControllerTests : IntegrationTestBase
    {
        public ServicesControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GenerateId_ReturnsOkAndIds()
        {
            // Act
            var response = await _client.GetAsync("api/services/generate-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.ValueKind.Should().Be(JsonValueKind.Object);
            
            // Verify properties exist
            result.Data.TryGetProperty("uniqueId", out _).Should().BeTrue();
            result.Data.TryGetProperty("guid", out _).Should().BeTrue();
        }
    }
}
