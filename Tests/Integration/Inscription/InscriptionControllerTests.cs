using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Responses;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Inscription
{
    public class InscriptionControllerTests : IntegrationTestBase
    {
        public InscriptionControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(Modality Modality, StateInscription State, AcademicPeriod Period, IdentificationType IdType, AcademicProgram Program, Role Role)> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var modality = new Modality { Name = "Modality " + Guid.NewGuid(), Description = "Desc", StatusRegister = true, MaxStudents = 5 };
            context.Set<Modality>().Add(modality);

            var state = new StateInscription { Code = StateInscriptionCodes.Pendiente, Name = "State " + Guid.NewGuid(), Description = "Desc", StatusRegister = true, IsInitialState = true };
            context.Set<StateInscription>().Add(state);

            var period = new AcademicPeriod { Code = "2025-" + Guid.NewGuid().ToString().Substring(0, 4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
            context.Set<AcademicPeriod>().Add(period);

            var idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" };
            if (!context.Set<IdentificationType>().Any(x => x.Code == "CC"))
                context.Set<IdentificationType>().Add(idType);
            else
                idType = context.Set<IdentificationType>().First(x => x.Code == "CC");

            var program = new AcademicProgram { Name = "Systems Engineering", Code = "SYS" };
            if (!context.Set<AcademicProgram>().Any(x => x.Code == "SYS"))
                context.Set<AcademicProgram>().Add(program);
            else
                program = context.Set<AcademicProgram>().First(x => x.Code == "SYS");

            var role = new Role { Name = "Student", Code = "STUDENT", Description = "Student Role", StatusRegister = true };
            if (!context.Set<Role>().Any(x => x.Code == "STUDENT"))
                context.Set<Role>().Add(role);
            else
                role = context.Set<Role>().First(x => x.Code == "STUDENT");

            await context.SaveChangesAsync();

            return (modality, state, period, idType, program, role);
        }

        [Fact]
        public async Task CreateInscription_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var (modality, _, period, idType, program, role) = await SeedDependenciesAsync();

            // Pre-seed a user to simulate an existing student
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var user1 = new User 
                { 
                    FirstName = "Existing", 
                    LastName = "Student", 
                    Email = "existing@test.com", 
                    Identification = "123456789", 
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id
                };
                context.Set<User>().Add(user1);
                context.Set<UserRole>().Add(new UserRole { IdUser = user1.Id, IdRole = role.Id, StatusRegister = true });

                var user2 = new User 
                { 
                    FirstName = "Second", 
                    LastName = "Student", 
                    Email = "second@test.com", 
                    Identification = "987654321", 
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id
                };
                context.Set<User>().Add(user2);
                context.Set<UserRole>().Add(new UserRole { IdUser = user2.Id, IdRole = role.Id, StatusRegister = true });

                await context.SaveChangesAsync();
            }

            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto 
                { 
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Test Inscription"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto { Identification = "123456789", IdIdentificationType = idType.Id }, // Existing
                    new UserInscriptionModalityCreateDto { Identification = "987654321", IdIdentificationType = idType.Id }  // New
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.InscriptionModality.IdModality.Should().Be(modality.Id);
            result.Data!.Students.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAll_ShouldReturnList()
        {
            // Arrange
            var (modality, state, period, idType, program, _) = await SeedDependenciesAsync();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = state.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Seeded Inscription"
                };
                context.Set<InscriptionModality>().Add(inscription);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/Inscription?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<InscriptionWithStudentsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetById_ShouldReturnDetails()
        {
            // Arrange
            var (modality, state, period, idType, program, _) = await SeedDependenciesAsync();
            int inscriptionId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = state.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Seeded Inscription"
                };
                context.Set<InscriptionModality>().Add(inscription);
                await context.SaveChangesAsync();
                inscriptionId = inscription.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/Inscription/{inscriptionId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.InscriptionModality.Id.Should().Be(inscriptionId);
        }

        [Fact]
        public async Task GetByUserId_ShouldReturnList()
        {
            // Arrange
            var (modality, state, period, idType, program, _) = await SeedDependenciesAsync();
            int userId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = new User 
                { 
                    FirstName = "Test", 
                    LastName = "User", 
                    Email = "test@test.com", 
                    Identification = "111222333", 
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id
                };
                context.Set<User>().Add(user);
                await context.SaveChangesAsync();
                userId = user.Id;

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = state.Id,
                    IdAcademicPeriod = period.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                
                var userInscription = new UserInscriptionModality
                {
                    IdUser = userId,
                    IdInscriptionModality = inscription.Id
                };
                context.Set<UserInscriptionModality>().Add(userInscription);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/Inscription/ByUser/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<InscriptionWithStudentsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data!.First().Students.Should().Contain(s => s.Id == userId);
        }

        [Fact]
        public async Task CreateInscription_WithEmptyStudentsList_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto { IdModality = 1 },
                Students = new List<UserInscriptionModalityCreateDto>() // Empty list
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", invalidDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            var jsonDoc = JsonDocument.Parse(content);
            var errors = jsonDoc.RootElement.GetProperty("Errors").EnumerateArray()
                .Select(e => e.GetString()).ToList();
            
            errors.Should().Contain(e => e.Contains("estudiante"));
        }

        [Fact]
        public async Task CreateInscription_WithInvalidStudentData_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto { IdModality = 1 },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto
                    {
                        Identification = "", // Invalid: Empty identification
                        IdIdentificationType = 0 // Invalid: 0
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", invalidDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            var jsonDoc = JsonDocument.Parse(content);
            var errors = jsonDoc.RootElement.GetProperty("Errors").EnumerateArray()
                .Select(e => e.GetString()).ToList();
            
            errors.Should().Contain(e => e.Contains("identificación"));
        }
    }
}
