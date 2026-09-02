using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.TeacherEnabledCargos;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.TeacherEnabledCargos
{
    public class TeacherEnabledCargoControllerTests : IntegrationTestBase
    {
        public TeacherEnabledCargoControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(User Teacher, List<TypeTeachingAssignment> Cargos)> SeedDataAsync()
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

            var teacher = new User
            {
                FirstName = "Test",
                LastName = "Teacher",
                Email = $"teacher_{Guid.NewGuid()}@test.com",
                Identification = Guid.NewGuid().ToString().Substring(0, 10),
                IdIdentificationType = identificationType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            var director = new TypeTeachingAssignment { Code = "DIRECTOR", Name = "Director", Description = "Director de tesis", MaxAssignments = 10, StatusRegister = true };
            var asesor = new TypeTeachingAssignment { Code = "ASESOR", Name = "Asesor", Description = "Asesor", MaxAssignments = 15, StatusRegister = true };
            var jurado = new TypeTeachingAssignment { Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", Description = "Jurado", MaxAssignments = 20, StatusRegister = true };

            context.Set<User>().Add(teacher);
            context.Set<TypeTeachingAssignment>().AddRange(director, asesor, jurado);
            await context.SaveChangesAsync();

            return (teacher, new List<TypeTeachingAssignment> { director, asesor, jurado });
        }

        [Fact]
        public async Task GetByUser_ReturnsOkAndEnabledCargos()
        {
            var (teacher, cargos) = await SeedDataAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
                {
                    IdUser = teacher.Id,
                    IdTypeTeachingAssignment = cargos[0].Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var response = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{teacher.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data![0].IdTypeTeachingAssignment.Should().Be(cargos[0].Id);
            result.Data[0].CargoName.Should().Be("Director");
            result.Data[0].CargoCode.Should().Be("DIRECTOR");
        }

        [Fact]
        public async Task GetByUser_WithNoCargos_ReturnsEmptyList()
        {
            var (teacher, _) = await SeedDataAsync();

            var response = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{teacher.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUser_WithInvalidId_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/TeacherEnabledCargo/ByUser/0");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static object AssignmentBody(int userId, List<int> ids) => new { userId, typeTeachingAssignmentIds = ids };

        [Fact]
        public async Task PostByUser_AssignsCargos_AndReturnsUpdatedList()
        {
            var (teacher, cargos) = await SeedDataAsync();
            var ids = new List<int> { cargos[0].Id, cargos[1].Id };

            var response = await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, ids));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task PostByUser_IsIdempotent_DoesNotDuplicate()
        {
            var (teacher, cargos) = await SeedDataAsync();
            var ids = new List<int> { cargos[0].Id };

            var response1 = await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, ids));
            var response2 = await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, ids));

            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{teacher.Id}");
            var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            result!.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task PostByUser_WithEmptyList_DeactivatesAllCargos()
        {
            var (teacher, cargos) = await SeedDataAsync();
            var ids = new List<int> { cargos[0].Id, cargos[1].Id };

            await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, ids));

            var emptyResponse = await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, new List<int>()));
            emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{teacher.Id}");
            var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            result!.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task PostByUser_ReplacesList_DeactivatesRemovedCargos()
        {
            var (teacher, cargos) = await SeedDataAsync();
            var first = new List<int> { cargos[0].Id, cargos[1].Id };

            await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, first));

            var second = new List<int> { cargos[2].Id };
            var response = await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", AssignmentBody(teacher.Id, second));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/api/TeacherEnabledCargo/ByUser/{teacher.Id}");
            var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            result!.Data.Should().HaveCount(1);
            result.Data[0].IdTypeTeachingAssignment.Should().Be(cargos[2].Id);
        }

        [Fact]
        public async Task PostByUser_WithInvalidId_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/api/TeacherEnabledCargo/ByUser", new { userId = 0, typeTeachingAssignmentIds = new List<int> { 1 } });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
