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
    }
}
