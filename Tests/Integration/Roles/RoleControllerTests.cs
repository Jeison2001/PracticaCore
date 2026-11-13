using System.Net;
using Application.Shared.DTOs.Roles;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.Roles
{
    public class RoleControllerTests : GenericControllerIntegrationTests<Role, RoleDto>
    {
        public RoleControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Role";

        protected override RoleDto CreateValidDto()
        {
            return new RoleDto
            {
                Name = "Administrator",
                Code = $"ADMIN_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "System Administrator",
                StatusRegister = true
            };
        }

        protected override Role CreateValidEntity()
        {
            return new Role
            {
                Name = "Administrator",
                Code = $"ADMIN_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "System Administrator",
                StatusRegister = true
            };
        }

        [Fact]
        public async Task GetAll_AndGetById_WhenUnauthenticated_ReturnUnauthorized()
        {
            using var getAllReq = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
            getAllReq.Headers.Add("X-Test-Anonymous", "true");
            (await _client.SendAsync(getAllReq)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            using var getByIdReq = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/1");
            getByIdReq.Headers.Add("X-Test-Anonymous", "true");
            (await _client.SendAsync(getByIdReq)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}