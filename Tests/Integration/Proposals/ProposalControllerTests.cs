using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Common.Services.Jobs;
using Application.Shared.DTOs.Proposals;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Common;
using Infrastructure.Data;
using Tests.Integration.Utilities;

namespace Tests.Integration.Proposals
{
    public class ProposalControllerTests : GenericControllerIntegrationTests<Proposal, ProposalDto>
    {
        protected override string BaseUrl => "/api/Proposal";

        public ProposalControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!context.Set<Modality>().Any())
                SeedingUtilities.SeedCatalogs(context);
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

        // Override GetById because it is [NonAction]
        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            var response = await _client.GetAsync($"{BaseUrl}/1");
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
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
            // Arrange - use CreateProposalDto (not ProposalDto) to match endpoint signature
            var dto = new CreateProposalDto
            {
                Title = "Test Proposal",
                GeneralObjective = "Test Objective",
                SpecificObjectives = new List<string> { "Obj 1", "Obj 2" },
                Description = "Test Description",
            };

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Create unique inscription ID (starts from high number to avoid conflicts)
                int inscriptionId = 5000;
                while (context.Set<InscriptionModality>().Local.Any(i => i.Id == inscriptionId) ||
                       await context.Set<InscriptionModality>().AnyAsync(i => i.Id == inscriptionId))
                    inscriptionId++;

                // Get or create AcademicPeriod
                var academicPeriod = context.Set<AcademicPeriod>().Local.FirstOrDefault()
                    ?? await context.Set<AcademicPeriod>().FirstOrDefaultAsync();
                if (academicPeriod == null)
                {
                    academicPeriod = new AcademicPeriod { Code = "2025-C", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                    context.Set<AcademicPeriod>().Add(academicPeriod);
                    await context.SaveChangesAsync();
                }

                // Create InscriptionModality
                var inscription = new InscriptionModality
                {
                    Id = inscriptionId,
                    IdModality = 1,  // Proyecto de Grado (seeded by SeedingUtilities in constructor)
                    IdStageModality = 4,  // PG_FASE_PROPUESTA (seeded by SeedingUtilities)
                    IdStateInscription = 2,  // Aprobado
                    IdAcademicPeriod = academicPeriod.Id,
                    StatusRegister = true,
                    OperationRegister = "Test"
                };
                context.Set<InscriptionModality>().Add(inscription);
                await context.SaveChangesAsync();

                // Create ResearchLine
                var researchLine = new ResearchLine { Name = "Line " + Guid.NewGuid(), Description = "Desc", StatusRegister = true, OperationRegister = "Test" };
                context.Set<ResearchLine>().Add(researchLine);
                await context.SaveChangesAsync();

                // Create ResearchSubLine
                var subLine = new ResearchSubLine { Name = "SubLine " + Guid.NewGuid(), Description = "Desc", IdResearchLine = researchLine.Id, StatusRegister = true, OperationRegister = "Test" };
                context.Set<ResearchSubLine>().Add(subLine);
                await context.SaveChangesAsync();

                dto.Id = inscription.Id;
                dto.IdResearchLine = researchLine.Id;
                dto.IdResearchSubLine = subLine.Id;
            }

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
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
            result.Data!.Items.First().Teachers.Should().ContainSingle(t => t.IdTeacher == teacherId);
        }

        [Fact]
        public async Task Update_AsMemberStudentInPropNoPertinente_SubmitsToPropRadicadaAndPreservesObservation()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int studentId;
            int noPertinenteStateId;
            int radicadaStateId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                noPertinenteStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropNoPertinente).Id;
                radicadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropRadicada).Id;

                entity.IdStateStage = noPertinenteStateId;
                entity.Observation = "Observacion original del comite";
                context.Set<Proposal>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var student = new User
                {
                    FirstName = "Subsanador",
                    LastName = "Student",
                    Email = $"sub_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);
                await context.SaveChangesAsync();
                studentId = student.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = entity.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.Title = "Titulo Subsanado";
            updateDto.IdResearchLine = entity.IdResearchLine;
            updateDto.IdResearchSubLine = entity.IdResearchSubLine;
            updateDto.IdStateStage = 30; // attempt to spoof PROP_PERTINENTE
            updateDto.Observation = "Estudiante intentando borrar observacion";

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{entity.Id}")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", studentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProposalDto>>();
            result!.Data!.Title.Should().Be("Titulo Subsanado");
            result.Data.IdStateStage.Should().Be(radicadaStateId);
            result.Data.Observation.Should().Be("Observacion original del comite");
        }

        [Fact]
        public async Task Update_AsNonMemberStudent_ReturnsForbidden()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int outsiderId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                entity.IdStateStage = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropNoPertinente).Id;
                context.Set<Proposal>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var outsider = new User
                {
                    FirstName = "Outsider",
                    LastName = "User",
                    Email = $"out_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1,
                    StatusRegister = true
                };
                context.Set<User>().Add(outsider);
                await context.SaveChangesAsync();
                outsiderId = outsider.Id;
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.IdResearchLine = entity.IdResearchLine;
            updateDto.IdResearchSubLine = entity.IdResearchSubLine;
            updateDto.IdStateStage = entity.IdStateStage;

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{entity.Id}")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", outsiderId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Update_AsMemberStudentWhenNotPropNoPertinente_ReturnsBadRequest()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int studentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                entity.IdStateStage = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropRadicada).Id;
                context.Set<Proposal>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var student = new User
                {
                    FirstName = "Member",
                    LastName = "User",
                    Email = $"mem_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);
                await context.SaveChangesAsync();
                studentId = student.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = entity.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.IdResearchLine = entity.IdResearchLine;
            updateDto.IdResearchSubLine = entity.IdResearchSubLine;
            updateDto.IdStateStage = entity.IdStateStage;

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{entity.Id}")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", studentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Patch_AsStudentWithN3PGRP_ReturnsForbidden()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int studentId;
            int radicadaStateId;
            int pertinenteStateId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                radicadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropRadicada).Id;
                pertinenteStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropPertinente).Id;

                entity.IdStateStage = radicadaStateId;
                context.Set<Proposal>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var student = new User
                {
                    FirstName = "Patch",
                    LastName = "Student",
                    Email = $"patch_stu_{Guid.NewGuid():N}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);
                await context.SaveChangesAsync();
                studentId = student.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = entity.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/{entity.Id}")
            {
                Content = JsonContent.Create(new ProposalPatchDto
                {
                    IdStateStage = pertinenteStateId,
                    Observation = "Autoaprobación indebida"
                })
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", studentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unchanged = await context.Set<Proposal>().FindAsync(entity.Id);
                unchanged!.IdStateStage.Should().Be(radicadaStateId);
            }
        }

        [Fact]
        public async Task Update_AsMemberStudentInPropNoPertinente_EnqueuesNotificationJob()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int studentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                entity.IdStateStage = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropNoPertinente).Id;
                entity.Observation = "Observacion comite";
                context.Set<Proposal>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var student = new User
                {
                    FirstName = "Notify",
                    LastName = "Student",
                    Email = $"notify_stu_{Guid.NewGuid():N}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);
                await context.SaveChangesAsync();
                studentId = student.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = entity.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            _factory.EnqueuedJobs.Clear();

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.Title = "Titulo Subsanado Con Notificacion";
            updateDto.IdResearchLine = entity.IdResearchLine;
            updateDto.IdResearchSubLine = entity.IdResearchSubLine;
            updateDto.IdStateStage = entity.IdStateStage;

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/{entity.Id}")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", studentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _factory.EnqueuedJobs.Should().ContainSingle(j =>
                j.MethodName == nameof(INotificationBackgroundJob.HandleProposalChangeAsync));
        }
    }
}