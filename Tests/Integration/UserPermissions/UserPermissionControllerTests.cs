using Application.Shared.DTOs.UserPermissions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using FluentAssertions;
using Domain.Entities;

namespace Tests.Integration.UserPermissions
{
    public class UserPermissionControllerTests : GenericControllerIntegrationTests<UserPermission, UserPermissionDto>
    {
        public UserPermissionControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/UserPermission";

        protected override UserPermissionDto CreateValidDto()
        {
            return new UserPermissionDto
            {
                IdUser = 0,
                IdPermission = 0,
                StatusRegister = true
            };
        }

        protected override UserPermission CreateValidEntity()
        {
            return new UserPermission
            {
                IdUser = 0,
                IdPermission = 0,
                StatusRegister = true
            };
        }

        protected override void SeedAdditionalData(UserPermission entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var faculty = new Faculty
                {
                    Name = "Test Faculty",
                    Code = $"F_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    StatusRegister = true
                };
                context.Set<Faculty>().Add(faculty);

                var program = new AcademicProgram
                {
                    Code = $"AP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Program",
                    Faculty = faculty,
                    StatusRegister = true
                };
                context.Set<AcademicProgram>().Add(program);

                var idType = new IdentificationType
                {
                    Name = "Test ID Type",
                    Code = $"IT_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test ID Type Desc",
                    StatusRegister = true
                };
                context.Set<IdentificationType>().Add(idType);

                var user = new User
                {
                    Identification = $"ID_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test.user@example.com",
                    IdentificationType = idType,
                    AcademicProgram = program,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);

                var permission = new Permission
                {
                    Code = $"P_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test Permission",
                    StatusRegister = true
                };
                context.Set<Permission>().Add(permission);

                context.SaveChanges();

                entity.IdUser = user.Id;
                entity.IdPermission = permission.Id;
            }
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            int userId, permissionId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var faculty = new Faculty
                {
                    Name = "Test Faculty",
                    Code = $"F_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    StatusRegister = true
                };
                context.Set<Faculty>().Add(faculty);

                var program = new AcademicProgram
                {
                    Code = $"AP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Program",
                    Faculty = faculty,
                    StatusRegister = true
                };
                context.Set<AcademicProgram>().Add(program);

                var idType = new IdentificationType
                {
                    Name = "Test ID Type",
                    Code = $"IT_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test ID Type Desc",
                    StatusRegister = true
                };
                context.Set<IdentificationType>().Add(idType);

                var user = new User
                {
                    Identification = $"ID_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test.user@example.com",
                    IdentificationType = idType,
                    AcademicProgram = program,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);

                var permission = new Permission
                {
                    Code = $"P_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test Permission",
                    StatusRegister = true
                };
                context.Set<Permission>().Add(permission);

                await context.SaveChangesAsync();
                
                userId = user.Id;
                permissionId = permission.Id;
            }

            var dto = new UserPermissionDto
            {
                IdUser = userId,
                IdPermission = permissionId,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserPermissionDto>>();
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
                context.Set<UserPermission>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new UserPermissionDto
            {
                Id = entity.Id,
                IdUser = entity.IdUser,
                IdPermission = entity.IdPermission,
                StatusRegister = false
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserPermissionDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
            result.Data.StatusRegister.Should().BeFalse();
        }
    }
}