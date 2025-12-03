using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Proposals;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Common;
using Infrastructure.Data;

namespace Tests.Integration.Proposals
{
    public class ProposalControllerTests : GenericControllerIntegrationTests<Proposal, ProposalDto>
    {
        protected override string BaseUrl => "/api/Proposal";

        public ProposalControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override ProposalDto CreateValidDto()
        {
            return new ProposalDto
            {
                Title = "Test Proposal",
                GeneralObjective = "Test Objective",
                SpecificObjectives = new List<string> { "Obj 1", "Obj 2" },
                Description = "Test Description",
            };
        }

        protected override Proposal CreateValidEntity()
        {
            return new Proposal
            {
                Title = "Test Proposal",
                GeneralObjective = "Test Objective",
                SpecificObjectives = new List<string> { "Obj 1", "Obj 2" },
                Description = "Test Description"
            };
        }

        protected override void SeedAdditionalData(Proposal entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Seed Dependencies for InscriptionModality
                var modality = new Modality { Name = "Test Modality " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                
                var stateInscription = new StateInscription { Name = "Test State " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                
                var academicPeriod = new AcademicPeriod { Code = "2025-" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                
                context.SaveChanges();

                // 2. Seed InscriptionModality (Parent of Proposal)
                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id,
                    Observations = "Seeded for Proposal Test"
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();
                
                // Assign the generated ID to the Proposal entity so it matches
                entity.Id = inscription.Id;

                // 3. Seed Dependencies for Proposal itself
                var researchLine = new ResearchLine { Name = "Line " + Guid.NewGuid(), Description = "Desc" };
                context.Set<ResearchLine>().Add(researchLine);
                context.SaveChanges();

                var subLine = new ResearchSubLine { Name = "SubLine " + Guid.NewGuid(), Description = "Desc", IdResearchLine = researchLine.Id };
                context.Set<ResearchSubLine>().Add(subLine);
                
                var stateStage = new StateStage { Name = "Stage " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                
                context.SaveChanges();

                // Update Entity FKs
                entity.IdResearchLine = researchLine.Id;
                entity.IdResearchSubLine = subLine.Id;
                entity.IdStateStage = stateStage.Id;
            }
        }

        // Override GetAll because it is [NonAction]
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<Proposal>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        // Override Create because we need to seed data BEFORE creating the DTO
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

                // Seed Proposal Dependencies
                var researchLine = new ResearchLine { Name = "Line Create " + Guid.NewGuid(), Description = "Desc" };
                context.Set<ResearchLine>().Add(researchLine);
                context.SaveChanges();
                var subLine = new ResearchSubLine { Name = "SubLine Create " + Guid.NewGuid(), Description = "Desc", IdResearchLine = researchLine.Id };
                context.Set<ResearchSubLine>().Add(subLine);
                var stateStage = new StateStage { Name = "Stage Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                context.SaveChanges();

                // Update DTO
                dto.Id = inscription.Id; // IMPORTANT: Proposal ID must match Inscription ID
                dto.IdResearchLine = researchLine.Id;
                dto.IdResearchSubLine = subLine.Id;
                dto.IdStateStage = stateStage.Id;
            }

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProposalDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(dto.Id);
        }

        // Override Update because we need to ensure the ID matches InscriptionModality
        public override async Task Update_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<Proposal>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.Title = "Updated Title";
            updateDto.IdResearchLine = entity.IdResearchLine;
            updateDto.IdResearchSubLine = entity.IdResearchSubLine;
            updateDto.IdStateStage = entity.IdStateStage;

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProposalDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
            result.Data!.Title.Should().Be("Updated Title");
        }



        [Fact]
        public async Task GetWithDetails_ReturnsOkAndEntity()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<Proposal>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProposalWithDetailsResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Proposal.Id.Should().Be(entity.Id);
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
                context.Set<Proposal>().Add(entity);
                
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
                // Ensure dependencies for User exist if needed (AcademicProgram, IdentificationType)
                // Assuming they might be needed, but for InMemory usually we can get away with just FKs if not enforcing constraints strictly or if we seed them.
                // Let's seed them to be safe.
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
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProposalWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data!.First().Proposal.Id.Should().Be(entity.Id);
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
                context.Set<Proposal>().Add(entity);
                
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
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data!.Items.First().Proposal.Id.Should().Be(entity.Id);
        }
    }
}