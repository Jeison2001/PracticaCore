using Application.Shared.DTOs.UserRole;
using Domain.Entities;
using Tests.Integration;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using FluentAssertions;

namespace Tests.Integration.UserRoles
{
    public class UserRoleControllerTests : GenericControllerIntegrationTests<Domain.Entities.UserRole, UserRoleDto>
    {
        public UserRoleControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/UserRole";

        protected override UserRoleDto CreateValidDto()
        {
            return new UserRoleDto
            {
                IdUser = 0,
                IdRole = 0,
                StatusRegister = true
            };
        }

        protected override Domain.Entities.UserRole CreateValidEntity()
        {
            return new Domain.Entities.UserRole
            {
                IdUser = 0,
                IdRole = 0,
                StatusRegister = true
            };
        }

        protected override void SeedAdditionalData(Domain.Entities.UserRole entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var faculty = new Domain.Entities.Faculty
                {
                    Name = "Test Faculty",
                    Code = $"F_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Faculty>().Add(faculty);

                var program = new Domain.Entities.AcademicProgram
                {
                    Code = $"AP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Program",
                    Faculty = faculty,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.AcademicProgram>().Add(program);

                var idType = new Domain.Entities.IdentificationType
                {
                    Name = "Test ID Type",
                    Code = $"IT_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test ID Type Desc",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.IdentificationType>().Add(idType);

                var user = new Domain.Entities.User
                {
                    Identification = $"ID_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test.user@example.com",
                    IdentificationType = idType,
                    AcademicProgram = program,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.User>().Add(user);

                var role = new Domain.Entities.Role
                {
                    Code = $"R_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Role",
                    Description = "Test Role Desc",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Role>().Add(role);

                context.SaveChanges();

                entity.IdUser = user.Id;
                entity.IdRole = role.Id;
            }
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            int userId, roleId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var faculty = new Domain.Entities.Faculty
                {
                    Name = "Test Faculty",
                    Code = $"F_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Faculty>().Add(faculty);

                var program = new Domain.Entities.AcademicProgram
                {
                    Code = $"AP_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Program",
                    Faculty = faculty,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.AcademicProgram>().Add(program);

                var idType = new Domain.Entities.IdentificationType
                {
                    Name = "Test ID Type",
                    Code = $"IT_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Description = "Test ID Type Desc",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.IdentificationType>().Add(idType);

                var user = new Domain.Entities.User
                {
                    Identification = $"ID_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test.user@example.com",
                    IdentificationType = idType,
                    AcademicProgram = program,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.User>().Add(user);

                var role = new Domain.Entities.Role
                {
                    Code = $"R_{Guid.NewGuid().ToString().Substring(0, 4)}",
                    Name = "Test Role",
                    Description = "Test Role Desc",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Role>().Add(role);

                await context.SaveChangesAsync();
                
                userId = user.Id;
                roleId = role.Id;
            }

            var dto = new UserRoleDto
            {
                IdUser = userId,
                IdRole = roleId,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserRoleDto>>();
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
                context.Set<Domain.Entities.UserRole>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new UserRoleDto
            {
                Id = entity.Id,
                IdUser = entity.IdUser,
                IdRole = entity.IdRole,
                StatusRegister = false
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserRoleDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
            result.Data.StatusRegister.Should().BeFalse();
        }
    }
}
