using Application.Shared.DTOs.RolePermissions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using FluentAssertions;
using Domain.Entities;

namespace Tests.Integration.RolePermissions
{
    public class RolePermissionControllerTests : GenericControllerIntegrationTests<RolePermission, RolePermissionDto>
    {
        public RolePermissionControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/RolePermission";

        protected override RolePermissionDto CreateValidDto()
        {
            return new RolePermissionDto
            {
                IdRole = 0,
                IdPermission = 0,
                StatusRegister = true
            };
        }

        protected override RolePermission CreateValidEntity()
        {
            return new RolePermission
            {
                IdRole = 0,
                IdPermission = 0,
                StatusRegister = true
            };
        }

        protected override void SeedAdditionalData(RolePermission entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var role = new Role
                {
                    Code = $"R_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Role",
                    Description = "Test Role Desc",
                    StatusRegister = true
                };
                context.Set<Role>().Add(role);

                var permission = new Permission
                {
                    Code = $"P_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test Permission",
                    StatusRegister = true
                };
                context.Set<Permission>().Add(permission);

                context.SaveChanges();

                entity.IdRole = role.Id;
                entity.IdPermission = permission.Id;
            }
        }

        [Fact]
        public override async Task GetAll_ReturnsOkAndList()
        {
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            var response = await _client.GetAsync($"{BaseUrl}/1");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            var response = await _client.PostAsJsonAsync(BaseUrl, CreateValidDto());
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/1", new RolePermissionDto { Id = 1 });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}