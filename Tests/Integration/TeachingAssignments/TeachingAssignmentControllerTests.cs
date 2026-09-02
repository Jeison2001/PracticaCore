using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.TeachingAssignments;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.TeachingAssignments
{
    public class TeachingAssignmentControllerTests : IntegrationTestBase
    {
        public TeachingAssignmentControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(User Teacher, InscriptionModality InscriptionModality, TypeTeachingAssignment TypeTeaching)> SeedDataAsync()
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
            
            var typeTeaching = new TypeTeachingAssignment { Name = "Director", Description = "Director de tesis", StatusRegister = true };
            
            var modality = new Modality { Name = "Pasantia", Description = "Pasantia", StatusRegister = true };
            var state = new StateInscription { Name = "Activo", Description = "Activo", StatusRegister = true };
            var period = new AcademicPeriod { Code = "2024-1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
            
            context.Set<Modality>().Add(modality);
            context.Set<StateInscription>().Add(state);
            context.Set<AcademicPeriod>().Add(period);
            context.Set<User>().Add(teacher);
            context.Set<TypeTeachingAssignment>().Add(typeTeaching);
            await context.SaveChangesAsync();

            var inscriptionModality = new InscriptionModality 
            { 
                IdModality = modality.Id,
                IdStateInscription = state.Id,
                IdAcademicPeriod = period.Id,
                StatusRegister = true
            };
            
            context.Set<InscriptionModality>().Add(inscriptionModality);
            await context.SaveChangesAsync();

            // Habilitar el cargo para el docente (TeacherEnabledCargo) — requerido por CreateTeachingAssignmentCommandHandler
            context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
            {
                IdUser = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = true
            });
            await context.SaveChangesAsync();

            return (teacher, inscriptionModality, typeTeaching);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenDataIsValid()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TeachingAssignment", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IdInscriptionModality.Should().Be(dto.IdInscriptionModality);
            result.Data.IdTeacher.Should().Be(dto.IdTeacher);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenCargoNotEnabledForTeacher()
        {
            // Arrange
            var (teacher, inscriptionModality, _) = await SeedDataAsync();

            // Cargo adicional que NO está habilitado para el docente
            int notEnabledCargoId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jurado = new TypeTeachingAssignment
                {
                    Code = "JURADO_EVALUADOR",
                    Name = "Jurado Evaluador",
                    Description = "Jurado",
                    MaxAssignments = 20,
                    StatusRegister = true
                };
                context.Set<TypeTeachingAssignment>().Add(jurado);
                await context.SaveChangesAsync();
                notEnabledCargoId = jurado.Id;
            }

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = notEnabledCargoId,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TeachingAssignment", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("no tiene habilitado el cargo"));
        }

        [Fact]
        public async Task GetByIdInscription_ShouldReturnList_WhenExists()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            
            // Create existing assignment
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/TeachingAssignment/ByInscription/{inscriptionModality.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeachingAssignmentTeacherDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenDataIsValid()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int assignmentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            var updateDto = new TeachingAssignmentDto
            {
                Id = assignmentId,
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = false // Changing status
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TeachingAssignment/{assignmentId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.StatusRegister.Should().BeFalse();
        }

        [Fact]
        public async Task GetAssignmentsByTeacherAndCargo_ReturnsAssignedProjectsWithModalityAndState()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            int assignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/TeachingAssignment/ByTeacherAndCargo/{teacher.Id}/{typeTeaching.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherAssignedProjectDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().ContainSingle(a => a.Id == assignmentId);

            var item = result.Data!.First(a => a.Id == assignmentId);
            item.IdInscriptionModality.Should().Be(inscriptionModality.Id);
            item.IdModality.Should().Be(inscriptionModality.IdModality);
            item.ModalityName.Should().Be("Pasantia");
            item.IdStateInscription.Should().Be(inscriptionModality.IdStateInscription);
            item.StateInscriptionName.Should().Be("Activo");
            item.IdTypeTeachingAssignment.Should().Be(typeTeaching.Id);
            item.CargoName.Should().Be("Director");
            item.StatusRegister.Should().BeTrue();
            item.RevocationDate.Should().BeNull();
        }

        [Fact]
        public async Task GetAssignmentsByTeacherAndCargo_FiltersRevokedByDefault_AndIncludesWhenRequested()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            int revokedAssignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var revoked = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    RevocationDate = DateTimeOffset.UtcNow,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(revoked);
                await context.SaveChangesAsync();
                revokedAssignmentId = revoked.Id;
            }

            // Act 1: Default (without includeRevoked)
            var response1 = await _client.GetAsync($"/api/TeachingAssignment/ByTeacherAndCargo/{teacher.Id}/{typeTeaching.Id}");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<List<TeacherAssignedProjectDto>>>();
            result1!.Data.Should().NotContain(a => a.Id == revokedAssignmentId);

            // Act 2: With includeRevoked = true
            var response2 = await _client.GetAsync($"/api/TeachingAssignment/ByTeacherAndCargo/{teacher.Id}/{typeTeaching.Id}?includeRevoked=true");
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<List<TeacherAssignedProjectDto>>>();
            result2!.Data.Should().Contain(a => a.Id == revokedAssignmentId);
        }

        [Fact]
        public async Task GetAssignmentsByTeacherAndCargo_WithInvalidIds_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/TeachingAssignment/ByTeacherAndCargo/0/1");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var response2 = await _client.GetAsync("/api/TeachingAssignment/ByTeacherAndCargo/1/0");
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_ReturnsCategorizedByCargo()
        {
            // Arrange
            var (teacher, inscriptionModality, typeDirector) = await SeedDataAsync();

            TypeTeachingAssignment typeAsesor;
            InscriptionModality inscription2;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                typeAsesor = new TypeTeachingAssignment { Code = "ASESOR", Name = "Asesor", Description = "Asesor", StatusRegister = true };
                context.Set<TypeTeachingAssignment>().Add(typeAsesor);

                inscription2 = new InscriptionModality
                {
                    IdModality = inscriptionModality.IdModality,
                    IdStateInscription = inscriptionModality.IdStateInscription,
                    IdAcademicPeriod = inscriptionModality.IdAcademicPeriod,
                    StatusRegister = true
                };
                context.Set<InscriptionModality>().Add(inscription2);
                await context.SaveChangesAsync();

                // Asignar docente como Director en inscriptionModality
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeDirector.Id,
                    StatusRegister = true
                });

                // Asignar docente como Asesor en inscription2
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscription2.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeAsesor.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/TeachingAssignment/ByTeacher/{teacher.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var directorGroup = result.Data!.FirstOrDefault(g => g.IdTypeTeachingAssignment == typeDirector.Id);
            directorGroup.Should().NotBeNull();
            directorGroup!.CargoName.Should().Be("Director");
            directorGroup.ActiveAssignmentsCount.Should().Be(1);
            directorGroup.Assignments.Should().ContainSingle(p => p.IdInscriptionModality == inscriptionModality.Id);

            var asesorGroup = result.Data!.FirstOrDefault(g => g.IdTypeTeachingAssignment == typeAsesor.Id);
            asesorGroup.Should().NotBeNull();
            asesorGroup!.CargoName.Should().Be("Asesor");
            asesorGroup.ActiveAssignmentsCount.Should().Be(1);
            asesorGroup.Assignments.Should().ContainSingle(p => p.IdInscriptionModality == inscription2.Id);
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_WithCargoFilter_ReturnsOnlyFilteredCargo()
        {
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var response = await _client.GetAsync($"/api/TeachingAssignment/ByTeacher/{teacher.Id}?idCargo={typeTeaching.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
            result!.Data.Should().ContainSingle(g => g.IdTypeTeachingAssignment == typeTeaching.Id);
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_WithInvalidId_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/TeachingAssignment/ByTeacher/0");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
