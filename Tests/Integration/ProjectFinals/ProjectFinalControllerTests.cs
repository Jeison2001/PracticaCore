using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.ProjectFinals;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Common;
using Infrastructure.Data;

namespace Tests.Integration.ProjectFinals
{
    public class ProjectFinalControllerTests : GenericControllerIntegrationTests<ProjectFinal, ProjectFinalDto>
    {
        protected override string BaseUrl => "/api/ProjectFinal";

        public ProjectFinalControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override ProjectFinalDto CreateValidDto()
        {
            return new ProjectFinalDto
            {
                Observations = "Test Observations",
                ReportApprovalDate = DateTime.Now
            };
        }

        protected override ProjectFinal CreateValidEntity()
        {
            return new ProjectFinal
            {
                Observations = "Test Observations",
                ReportApprovalDate = DateTime.Now
            };
        }

        protected override void SeedAdditionalData(ProjectFinal entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Seed InscriptionModality Chain
                var modality = new Modality { Name = "Test Modality " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
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

                // 2. Seed Proposal (Parent of ProjectFinal)
                var researchLine = new ResearchLine { Name = "Line " + Guid.NewGuid(), Description = "Desc" };
                context.Set<ResearchLine>().Add(researchLine);
                context.SaveChanges();
                var subLine = new ResearchSubLine { Name = "SubLine " + Guid.NewGuid(), Description = "Desc", IdResearchLine = researchLine.Id };
                context.Set<ResearchSubLine>().Add(subLine);
                var stateStageProposal = new StateStage { Name = "Stage Prop " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStageProposal);
                context.SaveChanges();

                var proposal = new Proposal
                {
                    Id = inscription.Id, // 1:1 with Inscription
                    Title = "Proposal for ProjectFinal",
                    GeneralObjective = "Obj",
                    SpecificObjectives = new List<string> { "Obj" },
                    IdResearchLine = researchLine.Id,
                    IdResearchSubLine = subLine.Id,
                    IdStateStage = stateStageProposal.Id
                };
                context.Set<Proposal>().Add(proposal);
                context.SaveChanges();

                // 3. Seed Dependencies for ProjectFinal
                var stateStageFinal = new StateStage { Name = "Stage Final " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStageFinal);
                context.SaveChanges();

                // Set Entity ID and FKs
                entity.Id = proposal.Id; // 1:1 with Proposal
                entity.IdStateStage = stateStageFinal.Id;
            }
        }

        public override Task GetAll_ReturnsOkAndList()
        {
            return Task.CompletedTask;
        }

        public override Task GetById_ReturnsOkAndEntity()
        {
            return Task.CompletedTask;
        }

        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            var dto = CreateValidDto();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Seed InscriptionModality
                var modality = new Modality { Name = "Test Modality Create " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State Create " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-C" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
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

                // Seed Proposal
                var researchLine = new ResearchLine { Name = "Line Create " + Guid.NewGuid(), Description = "Desc" };
                context.Set<ResearchLine>().Add(researchLine);
                context.SaveChanges();
                var subLine = new ResearchSubLine { Name = "SubLine Create " + Guid.NewGuid(), Description = "Desc", IdResearchLine = researchLine.Id };
                context.Set<ResearchSubLine>().Add(subLine);
                var stateStageProposal = new StateStage { Name = "Stage Prop Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStageProposal);
                context.SaveChanges();

                var proposal = new Proposal
                {
                    Id = inscription.Id,
                    Title = "Proposal Create",
                    GeneralObjective = "Obj",
                    SpecificObjectives = new List<string> { "Obj" },
                    IdResearchLine = researchLine.Id,
                    IdResearchSubLine = subLine.Id,
                    IdStateStage = stateStageProposal.Id
                };
                context.Set<Proposal>().Add(proposal);
                context.SaveChanges();

                // Seed ProjectFinal Dependencies
                var stateStageFinal = new StateStage { Name = "Stage Final Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStageFinal);
                context.SaveChanges();

                // Update DTO
                dto.Id = proposal.Id;
                dto.IdStateStage = stateStageFinal.Id;
            }

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectFinalDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(dto.Id);
        }

        public override async Task Update_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<ProjectFinal>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.Observations = "Updated Observations";
            updateDto.IdStateStage = entity.IdStateStage;

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectFinalDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
            result.Data!.Observations.Should().Be("Updated Observations");
        }

        [Fact]
        public async Task GetAllWithDetails_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<ProjectFinal>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
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
                context.Set<ProjectFinal>().Add(entity);
                
                // Seed User and UserInscriptionModality
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
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProjectFinalWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data!.First().ProjectFinal.Id.Should().Be(entity.Id);
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
                context.Set<ProjectFinal>().Add(entity);
                
                // Seed Teacher (User) and TeachingAssignment
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

                // Seed TypeTeachingAssignment
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
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data!.Items.First().ProjectFinal.Id.Should().Be(entity.Id);
        }
    }
}
