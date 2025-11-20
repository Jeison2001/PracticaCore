using Application.Shared.DTOs.Permissions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using FluentAssertions;
using Domain.Entities;

namespace Tests.Integration.Permissions
{
    public class PermissionControllerTests : GenericControllerIntegrationTests<Permission, PermissionDto>
    {
        public PermissionControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Permission";

        protected override PermissionDto CreateValidDto()
        {
            return new PermissionDto
            {
                Code = $"P_{Guid.NewGuid().ToString().Substring(0, 4)}",
                Description = "Test Permission",
                StatusRegister = true
            };
        }

        protected override Permission CreateValidEntity()
        {
            return new Permission
            {
                Code = $"P_{Guid.NewGuid().ToString().Substring(0, 4)}",
                Description = "Test Permission",
                StatusRegister = true
            };
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                context.Set<Permission>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.Code = entity.Code; // Keep the same Code to avoid key modification error

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
            result.Data.Code.Should().Be(entity.Code);
        }
    }
}