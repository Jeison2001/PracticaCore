using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.TeacherEnabledCargos;
using Application.Shared.DTOs.UserRoles;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.UserRoles
{
    /// <summary>
    /// Flujo compuesto: POST /api/UserRole con TypeTeachingAssignmentIds asigna rol + permisos
    /// + cargos habilitados en un solo commit (atómico).
    /// </summary>
    public class CreateUserRoleWithCargosTests : IntegrationTestBase
    {
        public CreateUserRoleWithCargosTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(User User, Role TeacherRole, Role StudentRole, List<TypeTeachingAssignment> Cargos, Permission Permission)> SeedDataAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var identificationType = new IdentificationType { Code = "CC", Name = "Cedula", Description = "Cedula", StatusRegister = true };
            var faculty = new Faculty { Code = "ING", Name = "Ingenieria", StatusRegister = true };
            var program = new AcademicProgram { Code = "SIS", Name = "Sistemas", Faculty = faculty, StatusRegister = true };

            context.Set<IdentificationType>().Add(identificationType);
            context.Set<Faculty>().Add(faculty);
            context.Set<AcademicProgram>().Add(program);
            await context.SaveChangesAsync();

            var user = new User
            {
                FirstName = "Test",
                LastName = "User",
                Email = $"user_{Guid.NewGuid()}@test.com",
                Identification = Guid.NewGuid().ToString().Substring(0, 10),
                IdIdentificationType = identificationType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            var teacherRole = new Role { Code = "TEACHER", Name = "Docente", Description = "Docente", StatusRegister = true };
            var studentRole = new Role { Code = "STUDENT", Name = "Estudiante", Description = "Estudiante", StatusRegister = true };

            var director = new TypeTeachingAssignment { Code = "DIRECTOR", Name = "Director", Description = "Director de tesis", MaxAssignments = 10, StatusRegister = true };
            var asesor = new TypeTeachingAssignment { Code = "ASESOR", Name = "Asesor", Description = "Asesor", MaxAssignments = 15, StatusRegister = true };

            var permission = new Permission { Code = "P_TEST", Description = "Permiso Test", StatusRegister = true };

            context.Set<User>().Add(user);
            context.Set<Role>().AddRange(teacherRole, studentRole);
            context.Set<TypeTeachingAssignment>().AddRange(director, asesor);
            context.Set<Permission>().Add(permission);
            await context.SaveChangesAsync();

            // Permiso del rol TEACHER para verificar la asignación automática de permisos en el mismo commit
            context.Set<RolePermission>().Add(new RolePermission
            {
                IdRole = teacherRole.Id,
                IdPermission = permission.Id,
                StatusRegister = true
            });
            await context.SaveChangesAsync();

            return (user, teacherRole, studentRole, new List<TypeTeachingAssignment> { director, asesor }, permission);
        }

        [Fact]
        public async Task Create_WithTeacherRoleAndCargos_AssignsRolePermisosAndCargosAtomically()
        {
            var (user, teacherRole, _, cargos, permission) = await SeedDataAsync();
            var dto = new
            {
                idUser = user.Id,
                idRole = teacherRole.Id,
                typeTeachingAssignmentIds = new List<int> { cargos[0].Id, cargos[1].Id }
            };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Cargos habilitados persistidos
            var cargosResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{user.Id}");
            var cargosResult = await cargosResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            cargosResult!.Data.Should().HaveCount(2);

            // Permisos del rol asignados automáticamente (UserRoleAssignedEvent en el mismo commit)
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userPermission = await context.Set<UserPermission>()
                    .FirstOrDefaultAsync(up => up.IdUser == user.Id && up.IdPermission == permission.Id);
                userPermission.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Create_WithTeacherRoleWithoutCargosField_ReturnsBadRequest()
        {
            var (user, teacherRole, _, _, _) = await SeedDataAsync();
            var dto = new { idUser = user.Id, idRole = teacherRole.Id };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserRoleDto>>();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("al menos un cargo"));

            // Nada persistido (primera validación, atómico): ni rol ni cargos
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userRole = await context.Set<UserRole>()
                    .FirstOrDefaultAsync(ur => ur.IdUser == user.Id && ur.IdRole == teacherRole.Id);
                userRole.Should().BeNull();
            }

            var cargosResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{user.Id}");
            var cargosResult = await cargosResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            cargosResult!.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_WithTeacherRoleAndEmptyCargosList_ReturnsBadRequest()
        {
            var (user, teacherRole, _, _, _) = await SeedDataAsync();
            var dto = new
            {
                idUser = user.Id,
                idRole = teacherRole.Id,
                typeTeachingAssignmentIds = new List<int>()
            };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserRoleDto>>();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("al menos un cargo"));

            // Nada persistido (primera validación, atómico): ni rol ni cargos
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userRole = await context.Set<UserRole>()
                    .FirstOrDefaultAsync(ur => ur.IdUser == user.Id && ur.IdRole == teacherRole.Id);
                userRole.Should().BeNull();
            }

            var cargosResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{user.Id}");
            var cargosResult = await cargosResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            cargosResult!.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_WithNonTeacherRoleAndEmptyCargosList_AssignsRoleOnly()
        {
            var (user, _, studentRole, _, _) = await SeedDataAsync();
            var dto = new
            {
                idUser = user.Id,
                idRole = studentRole.Id,
                typeTeachingAssignmentIds = new List<int>()
            };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);

            // Lista vacía con rol no-TEACHER no dispara validación (compatibilidad)
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userRole = await context.Set<UserRole>()
                    .FirstOrDefaultAsync(ur => ur.IdUser == user.Id && ur.IdRole == studentRole.Id);
                userRole.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Create_WithNonTeacherRoleAndCargos_ReturnsBadRequest()
        {
            var (user, _, studentRole, cargos, _) = await SeedDataAsync();
            var dto = new
            {
                idUser = user.Id,
                idRole = studentRole.Id,
                typeTeachingAssignmentIds = new List<int> { cargos[0].Id }
            };

            var response = await _client.PostAsJsonAsync("/api/UserRole", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserRoleDto>>();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("solo aplican a docentes"));

            // No debe quedar nada persistido (atómico)
            var cargosResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{user.Id}");
            var cargosResult = await cargosResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            cargosResult!.Data.Should().BeEmpty();
        }
    }
}
