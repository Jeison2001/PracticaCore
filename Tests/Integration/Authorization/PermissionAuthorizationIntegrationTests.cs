using System.Net;
using System.Net.Http.Json;
using Domain.Constants;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.Authorization
{
    public class PermissionAuthorizationIntegrationTests : IntegrationTestBase
    {
        public PermissionAuthorizationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Theory]
        [InlineData("GET", "/api/Dashboard/filters")]
        [InlineData("GET", "/api/RolePermission/ByRole?id=1")]
        [InlineData("GET", "/api/Proposal/GetAll")]
        [InlineData("GET", "/api/AcademicAverage/GetAll")]
        [InlineData("POST", "/api/Modality")]
        public async Task ProtectedEndpoints_WithNoPermissions_ReturnForbidden(string method, string url)
        {
            using var request = CreateRequest(method, url, "NONE");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task StudentWithoutManagementPermissions_GetsForbidden_OnGetAllEndpoints()
        {
            var studentPermissions = string.Join(",",
                PermissionCodes.Configuraciones.N2CFP,
                PermissionCodes.ModalidadesGrado.N2MGR,
                PermissionCodes.ModalidadesGrado.N2MGC,
                PermissionCodes.ProyectoGrado.N4PGPPC);

            using var proposalReq = CreateRequest("GET", "/api/Proposal/GetAll", studentPermissions);
            using var averageReq = CreateRequest("GET", "/api/AcademicAverage/GetAll", studentPermissions);

            var proposalResponse = await _client.SendAsync(proposalReq);
            var averageResponse = await _client.SendAsync(averageReq);

            proposalResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            averageResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UserWithOnlyOwnProfilePermission_GetsForbidden_WhenRequestingAnotherUser()
        {
            using var request = CreateRequest("GET", "/api/User/999", PermissionCodes.Configuraciones.N2CFP);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task TeacherWithByTeacherPermission_GetsOkOnByTeacher_AndForbiddenOnGetAll()
        {
            using var byTeacherReq = CreateRequest("GET", "/api/Proposal/ByTeacher/1", PermissionCodes.ProyectoGrado.N4PGPPC);
            using var getAllReq = CreateRequest("GET", "/api/Proposal/GetAll", PermissionCodes.ProyectoGrado.N4PGPPC);

            var byTeacherResponse = await _client.SendAsync(byTeacherReq);
            var getAllResponse = await _client.SendAsync(getAllReq);

            byTeacherResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getAllResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AllowAnonymousEndpoint_WithNoPermissions_ReturnsOk()
        {
            using var request = CreateRequest("GET", "/api/Role", "NONE");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static HttpRequestMessage CreateRequest(string method, string url, string permissionsHeader)
        {
            var httpMethod = method.ToUpperInvariant() switch
            {
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "PATCH" => HttpMethod.Patch,
                _ => HttpMethod.Get
            };

            var request = new HttpRequestMessage(httpMethod, url);
            request.Headers.Add("X-Test-Permissions", permissionsHeader);

            if (httpMethod == HttpMethod.Post)
            {
                request.Content = JsonContent.Create(new { name = "Test", code = "TST", description = "Test" });
            }

            return request;
        }
    }
}
