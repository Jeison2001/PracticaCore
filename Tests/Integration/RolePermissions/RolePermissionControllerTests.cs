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
        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            int roleId, permissionId;
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
                await context.SaveChangesAsync();
                
                roleId = role.Id;
                permissionId = permission.Id;
            }

            var dto = new RolePermissionDto
            {
                IdRole = roleId,
                IdPermission = permissionId,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RolePermissionDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
             // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                context.Set<RolePermission>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new RolePermissionDto
            {
                Id = entity.Id,
                IdRole = entity.IdRole,
                IdPermission = entity.IdPermission,
                StatusRegister = false
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RolePermissionDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
            result.Data.StatusRegister.Should().BeFalse();
        }
    }
}