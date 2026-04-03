using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.AcademicPractices;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Common;
using Infrastructure.Data;

namespace Tests.Integration.AcademicPractices
{
    public class AcademicPracticeControllerTests : GenericControllerIntegrationTests<AcademicPractice, AcademicPracticeDto>
    {
        protected override string BaseUrl => "/api/AcademicPractice";

        public AcademicPracticeControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override AcademicPracticeDto CreateValidDto()
        {
            return new AcademicPracticeDto
            {
                Title = "Test Practice",
                InstitutionName = "Test Inst",
                InstitutionContact = "Contact",
                PracticeStartDate = DateTime.Now,
                PracticeEndDate = DateTime.Now.AddMonths(3),
                PracticeHours = 100,
                Observations = "Obs"
            };
        }

        protected override AcademicPractice CreateValidEntity()
        {
            return new AcademicPractice
            {
                Title = "Test Practice",
                InstitutionName = "Test Inst",
                InstitutionContact = "Contact",
                PracticeStartDate = DateTime.Now,
                PracticeEndDate = DateTime.Now.AddMonths(3),
                PracticeHours = 100,
                Observations = "Obs"
            };
        }

        protected override void SeedAdditionalData(AcademicPractice entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Seed de cadena InscriptionModality
                var modality = new Modality { Name = "Test Modality AP " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State AP " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-AP" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                // 2. Seed de StateStage
                var stateStage = new StateStage { Name = "Stage AP " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                context.SaveChanges();

                // Establecer ID de entidad y FKs
                entity.Id = inscription.Id; // 1:1 with Inscription
                entity.IdStateStage = stateStage.Id;
            }
        }

        // Override de GetAll porque el controlador usa un endpoint personalizado /GetAll (WithDetails)
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        // Override de GetById porque el controlador usa /WithDetails/{id}
        public override async Task GetById_ReturnsOkAndEntity()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeWithDetailsResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AcademicPractice!.Id.Should().Be(entity.Id);
        }

        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            var dto = CreateValidDto();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Seed de InscriptionModality
                var modality = new Modality { Name = "Test Modality AP Create " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State AP Create " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-APC" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                // Seed de StateStage
                var stateStage = new StateStage { Name = "Stage AP Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                context.SaveChanges();

                // Actualizar DTO
                dto.Id = inscription.Id;
                dto.IdStateStage = stateStage.Id;
            }

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(dto.Id);
        }

        [Fact]
        public async Task UpdateInstitutionInfo_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new UpdateInstitutionInfoDto
            {
                Id = entity.Id,
                Title = "Updated Title",
                InstitutionName = "Updated Inst",
                InstitutionContact = "Updated Contact",
                PracticeStartDate = DateTime.Now.AddDays(1),
                PracticeEndDate = DateTime.Now.AddDays(5),
                PracticeHours = 80
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdateInstitutionInfo/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();

            // Verificar en BD
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dbEntity = await context.Set<AcademicPractice>().FindAsync(entity.Id);
                dbEntity.Should().NotBeNull();
                dbEntity!.Title.Should().Be("Updated Title");
                dbEntity.PracticeHours.Should().Be(80);
            }
        }

        [Fact]
        public async Task UpdatePhaseApproval_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage, // Usar estado existente o uno nuevo
                Observations = "Approved",
                EvaluatorObservations = "Approved"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetByUserId_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int userId = 1;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                
                // Seed de User y UserInscriptionModality
                var user = new User 
                { 
                    Id = userId,
                    FirstName = "Test", 
                    LastName = "User", 
                    Email = "test@test.com", 
                    Identification = "123",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                context.Set<User>().Add(user);

                var userInscription = new UserInscriptionModality
                {
                    IdUser = userId,
                    IdInscriptionModality = entity.Id
                };
                context.Set<UserInscriptionModality>().Add(userInscription);

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByUser/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data!.First().AcademicPractice!.Id.Should().Be(entity.Id);
        }

        [Fact]
        public async Task GetByTeacherId_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int teacherId = 2;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                
                // Seed de Teacher (User) y TeachingAssignment
                var teacher = new User 
                { 
                    Id = teacherId,
                    FirstName = "Teacher", 
                    LastName = "User", 
                    Email = "teacher@test.com", 
                    Identification = "456",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<User>().Any(u => u.Id == teacherId))
                    context.Set<User>().Add(teacher);

                // Seed de TypeTeachingAssignment
                var typeAssignment = new TypeTeachingAssignment { Name = "Director", Description = "Director" };
                context.Set<TypeTeachingAssignment>().Add(typeAssignment);
                await context.SaveChangesAsync();

                var assignment = new TeachingAssignment
                {
                    IdTeacher = teacherId,
                    IdInscriptionModality = entity.Id,
                    IdTypeTeachingAssignment = typeAssignment.Id
                };
                context.Set<TeachingAssignment>().Add(assignment);

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByTeacher/{teacherId}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data!.Items.First().AcademicPractice!.Id.Should().Be(entity.Id);
        }
    }
}
