using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Documents;
using Application.Shared.DTOs.PreliminaryProjects;
using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.DTOs.ProjectPresentations;
using Application.Shared.DTOs.Proposals;
using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Services.Storage;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.Authorization
{
    public class ActorProyectoGradoEndpointsMatrixTests
    {
        private const int Student101Id = 101;
        private const int Student102Id = 102;
        private const int Teacher201Id = 201;
        private const int Teacher202Id = 202;
        private const int Teacher203Id = 203;

        private sealed record SeededMatrixScenario(
            int InscriptionAId,
            int InscriptionBId,
            int DirectorCargoId,
            int JuradoCargoId,
            int Assignment202Id,
            int PropRadicadaStateId,
            int ResearchLineId,
            int ResearchSubLineId,
            string ProposalAObservation);

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            int userId,
            params string[] permissions)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-UserId", userId.ToString());
            request.Headers.Add(
                "X-Test-Permissions",
                permissions.Length == 0 ? "NONE" : string.Join(",", permissions));
            return request;
        }

        private static async Task<SeededMatrixScenario> SeedScenarioAsync(
            WebApplicationFactory<Program> factory,
            string proposalAStateCode = StateStageCodes.PropRadicada)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            SeedingUtilities.SeedCatalogs(context);

            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

            var idType = new IdentificationType
            {
                Code = $"CC_{uniqueSuffix}",
                Name = "Cédula de Ciudadanía",
                Description = "Documento",
                StatusRegister = true
            };
            var faculty = new Faculty
            {
                Code = $"ING_{uniqueSuffix}",
                Name = "Facultad de Ingeniería",
                StatusRegister = true
            };
            var program = new AcademicProgram
            {
                Code = $"SIS_{uniqueSuffix}",
                Name = "Ingeniería de Sistemas",
                Faculty = faculty,
                StatusRegister = true
            };
            var period = new AcademicPeriod
            {
                Code = $"2026-1-{uniqueSuffix}",
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddMonths(5),
                StatusRegister = true
            };

            context.Set<IdentificationType>().Add(idType);
            context.Set<Faculty>().Add(faculty);
            context.Set<AcademicProgram>().Add(program);
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            var users = new[]
            {
                new User
                {
                    Id = Student101Id,
                    FirstName = "Estudiante",
                    LastName = "Uno",
                    Email = $"est101_{uniqueSuffix}@unicesar.edu.co",
                    Identification = $"101{uniqueSuffix}",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                },
                new User
                {
                    Id = Student102Id,
                    FirstName = "Estudiante",
                    LastName = "Dos",
                    Email = $"est102_{uniqueSuffix}@unicesar.edu.co",
                    Identification = $"102{uniqueSuffix}",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                },
                new User
                {
                    Id = Teacher201Id,
                    FirstName = "Docente",
                    LastName = "Confirmado",
                    Email = $"doc201_{uniqueSuffix}@unicesar.edu.co",
                    Identification = $"201{uniqueSuffix}",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                },
                new User
                {
                    Id = Teacher202Id,
                    FirstName = "Docente",
                    LastName = "Preseleccionado",
                    Email = $"doc202_{uniqueSuffix}@unicesar.edu.co",
                    Identification = $"202{uniqueSuffix}",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                },
                new User
                {
                    Id = Teacher203Id,
                    FirstName = "Docente",
                    LastName = "Ajeno",
                    Email = $"doc203_{uniqueSuffix}@unicesar.edu.co",
                    Identification = $"203{uniqueSuffix}",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                }
            };
            context.Set<User>().AddRange(users);

            var directorCargo = new TypeTeachingAssignment
            {
                Code = "DIRECTOR",
                Name = "Director",
                Description = "Director de Proyecto de Grado",
                MaxAssignments = 5,
                StatusRegister = true
            };
            var juradoCargo = new TypeTeachingAssignment
            {
                Code = "JURADO_EVALUADOR",
                Name = "Jurado Evaluador",
                Description = "Jurado Evaluador",
                MaxAssignments = 5,
                StatusRegister = true
            };
            context.Set<TypeTeachingAssignment>().AddRange(directorCargo, juradoCargo);

            var researchLine = new ResearchLine
            {
                Name = $"Línea IA {uniqueSuffix}",
                Description = "Inteligencia Artificial",
                StatusRegister = true
            };
            context.Set<ResearchLine>().Add(researchLine);
            await context.SaveChangesAsync();

            var researchSubLine = new ResearchSubLine
            {
                Name = $"Sublínea NLP {uniqueSuffix}",
                Description = "Procesamiento de Lenguaje",
                IdResearchLine = researchLine.Id,
                StatusRegister = true
            };
            context.Set<ResearchSubLine>().Add(researchSubLine);

            foreach (var teacherId in new[] { Teacher201Id, Teacher202Id, Teacher203Id })
            {
                context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
                {
                    IdUser = teacherId,
                    IdTypeTeachingAssignment = directorCargo.Id,
                    StatusRegister = true
                });
                context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
                {
                    IdUser = teacherId,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    StatusRegister = true
                });
            }
            await context.SaveChangesAsync();

            var modality = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado);
            var approvedState = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado);
            var stageSust = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion);

            var propRadicadaState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropRadicada);
            var propAState = context.Set<StateStage>().First(s => s.Code == proposalAStateCode);
            var apEnEvalState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApEnEvaluacion);
            var apPendAsigState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval);
            var pfEnEvalState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfRadicadoEnEvaluacion);
            var sustProgramadaState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada);
            var sustPendienteState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustPendienteProgramacion);

            var inscriptionA = new InscriptionModality
            {
                IdModality = modality.Id,
                IdStateInscription = approvedState.Id,
                IdAcademicPeriod = period.Id,
                IdStageModality = stageSust.Id,
                Observations = $"Inscripción A {uniqueSuffix}",
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var inscriptionB = new InscriptionModality
            {
                IdModality = modality.Id,
                IdStateInscription = approvedState.Id,
                IdAcademicPeriod = period.Id,
                IdStageModality = stageSust.Id,
                Observations = $"Inscripción B {uniqueSuffix}",
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Set<InscriptionModality>().AddRange(inscriptionA, inscriptionB);
            await context.SaveChangesAsync();

            context.Set<UserInscriptionModality>().AddRange(
                new UserInscriptionModality
                {
                    IdInscriptionModality = inscriptionA.Id,
                    IdUser = Student101Id,
                    StatusRegister = true
                },
                new UserInscriptionModality
                {
                    IdInscriptionModality = inscriptionB.Id,
                    IdUser = Student102Id,
                    StatusRegister = true
                });

            const string proposalAObservation = "Corregir metodología y alcance";
            context.Set<Proposal>().AddRange(
                new Proposal
                {
                    Id = inscriptionA.Id,
                    Title = $"Propuesta A {uniqueSuffix}",
                    Description = "Descripción Propuesta A",
                    Observation = proposalAObservation,
                    GeneralObjective = "Objetivo A",
                    SpecificObjectives = new List<string> { "Obj A1" },
                    IdResearchLine = researchLine.Id,
                    IdResearchSubLine = researchSubLine.Id,
                    IdStateStage = propAState.Id,
                    StatusRegister = true
                },
                new Proposal
                {
                    Id = inscriptionB.Id,
                    Title = $"Propuesta B {uniqueSuffix}",
                    Description = "Descripción Propuesta B",
                    Observation = "Sin observaciones",
                    GeneralObjective = "Objetivo B",
                    SpecificObjectives = new List<string> { "Obj B1" },
                    IdResearchLine = researchLine.Id,
                    IdResearchSubLine = researchSubLine.Id,
                    IdStateStage = propRadicadaState.Id,
                    StatusRegister = true
                });

            context.Set<PreliminaryProject>().AddRange(
                new PreliminaryProject
                {
                    Id = inscriptionA.Id,
                    IdStateStage = apEnEvalState.Id,
                    Observations = "Anteproyecto A en evaluación",
                    StatusRegister = true
                },
                new PreliminaryProject
                {
                    Id = inscriptionB.Id,
                    IdStateStage = apPendAsigState.Id,
                    Observations = "Anteproyecto B pendiente asignación",
                    StatusRegister = true
                });

            context.Set<ProjectFinal>().AddRange(
                new ProjectFinal
                {
                    Id = inscriptionA.Id,
                    IdStateStage = pfEnEvalState.Id,
                    Observations = "Proyecto Final A",
                    StatusRegister = true
                },
                new ProjectFinal
                {
                    Id = inscriptionB.Id,
                    IdStateStage = pfEnEvalState.Id,
                    Observations = "Proyecto Final B",
                    StatusRegister = true
                });

            context.Set<ProjectPresentation>().AddRange(
                new ProjectPresentation
                {
                    Id = inscriptionA.Id,
                    IdInscriptionModality = inscriptionA.Id,
                    IdStateStage = sustProgramadaState.Id,
                    AttemptNumber = 1,
                    ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(5),
                    PresentationModality = "PRESENCIAL",
                    LocationOrLink = "Auditorio Principal",
                    Observations = "Sustentación A",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new ProjectPresentation
                {
                    Id = inscriptionB.Id,
                    IdInscriptionModality = inscriptionB.Id,
                    IdStateStage = sustPendienteState.Id,
                    AttemptNumber = 1,
                    Observations = "Sustentación B",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });

            var assignment201 = new TeachingAssignment
            {
                IdInscriptionModality = inscriptionA.Id,
                IdTeacher = Teacher201Id,
                IdTypeTeachingAssignment = directorCargo.Id,
                IsConfirmed = true,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var assignment202 = new TeachingAssignment
            {
                IdInscriptionModality = inscriptionA.Id,
                IdTeacher = Teacher202Id,
                IdTypeTeachingAssignment = directorCargo.Id,
                IsConfirmed = false,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var assignment203 = new TeachingAssignment
            {
                IdInscriptionModality = inscriptionB.Id,
                IdTeacher = Teacher203Id,
                IdTypeTeachingAssignment = directorCargo.Id,
                IsConfirmed = true,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Set<TeachingAssignment>().AddRange(assignment201, assignment202, assignment203);

            await context.SaveChangesAsync();

            return new SeededMatrixScenario(
                inscriptionA.Id,
                inscriptionB.Id,
                directorCargo.Id,
                juradoCargo.Id,
                assignment202.Id,
                propRadicadaState.Id,
                researchLine.Id,
                researchSubLine.Id,
                proposalAObservation);
        }

        [Fact]
        public async Task Teacher201Confirmed_Vs_Teacher202Unconfirmed_OnByTeacherEndpoints_ReturnsExpectedProjectsAndTeachers()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            // 1. Proposal/ByTeacher
            using (var req201 = CreateRequest(HttpMethod.Get, $"/api/Proposal/ByTeacher/{Teacher201Id}?PageNumber=1&PageSize=10", Teacher201Id, PermissionCodes.ProyectoGrado.N4PGPPC))
            using (var req202 = CreateRequest(HttpMethod.Get, $"/api/Proposal/ByTeacher/{Teacher202Id}?PageNumber=1&PageSize=10", Teacher202Id, PermissionCodes.ProyectoGrado.N4PGPPC))
            {
                var resp201 = await client.SendAsync(req201);
                var resp202 = await client.SendAsync(req202);

                resp201.StatusCode.Should().Be(HttpStatusCode.OK);
                resp202.StatusCode.Should().Be(HttpStatusCode.OK);

                var body201 = await resp201.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>>>();
                var body202 = await resp202.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>>>();

                body201!.Data!.Items.Should().ContainSingle(x => x.Proposal.Id == scenario.InscriptionAId);
                var item201 = body201.Data.Items.Single();
                item201.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                item201.Teachers.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                item201.Teachers.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                body202!.Data!.Items.Should().BeEmpty();
            }

            // 2. PreliminaryProject/ByTeacher
            using (var req201 = CreateRequest(HttpMethod.Get, $"/api/PreliminaryProject/ByTeacher/{Teacher201Id}?PageNumber=1&PageSize=10", Teacher201Id, PermissionCodes.ProyectoGrado.N4PGPAR))
            using (var req202 = CreateRequest(HttpMethod.Get, $"/api/PreliminaryProject/ByTeacher/{Teacher202Id}?PageNumber=1&PageSize=10", Teacher202Id, PermissionCodes.ProyectoGrado.N4PGPAR))
            {
                var resp201 = await client.SendAsync(req201);
                var resp202 = await client.SendAsync(req202);

                resp201.StatusCode.Should().Be(HttpStatusCode.OK);
                resp202.StatusCode.Should().Be(HttpStatusCode.OK);

                var body201 = await resp201.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>>();
                var body202 = await resp202.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>>();

                body201!.Data!.Items.Should().ContainSingle(x => x.PreliminaryProject.Id == scenario.InscriptionAId);
                var item201 = body201.Data.Items.Single();
                item201.Proposal.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                item201.Proposal.Teachers.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                item201.Proposal.Teachers.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                body202!.Data!.Items.Should().BeEmpty();
            }

            // 3. ProjectFinal/ByTeacher
            using (var req201 = CreateRequest(HttpMethod.Get, $"/api/ProjectFinal/ByTeacher/{Teacher201Id}?PageNumber=1&PageSize=10", Teacher201Id, PermissionCodes.ProyectoGrado.N4PGPPRR))
            using (var req202 = CreateRequest(HttpMethod.Get, $"/api/ProjectFinal/ByTeacher/{Teacher202Id}?PageNumber=1&PageSize=10", Teacher202Id, PermissionCodes.ProyectoGrado.N4PGPPRR))
            {
                var resp201 = await client.SendAsync(req201);
                var resp202 = await client.SendAsync(req202);

                resp201.StatusCode.Should().Be(HttpStatusCode.OK);
                resp202.StatusCode.Should().Be(HttpStatusCode.OK);

                var body201 = await resp201.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>>>();
                var body202 = await resp202.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>>>();

                body201!.Data!.Items.Should().ContainSingle(x => x.ProjectFinal.Id == scenario.InscriptionAId);
                var item201 = body201.Data.Items.Single();
                item201.Proposal.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                item201.Proposal.Teachers.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                item201.Proposal.Teachers.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                body202!.Data!.Items.Should().BeEmpty();
            }

            // 4. ProjectPresentation/ByTeacher
            using (var req201 = CreateRequest(HttpMethod.Get, $"/api/ProjectPresentation/ByTeacher/{Teacher201Id}?PageNumber=1&PageSize=10", Teacher201Id, PermissionCodes.ProyectoGrado.N4PGPSR))
            using (var req202 = CreateRequest(HttpMethod.Get, $"/api/ProjectPresentation/ByTeacher/{Teacher202Id}?PageNumber=1&PageSize=10", Teacher202Id, PermissionCodes.ProyectoGrado.N4PGPSR))
            {
                var resp201 = await client.SendAsync(req201);
                var resp202 = await client.SendAsync(req202);

                resp201.StatusCode.Should().Be(HttpStatusCode.OK);
                resp202.StatusCode.Should().Be(HttpStatusCode.OK);

                var body201 = await resp201.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectPresentationWithDetailsResponseDto>>>();
                var body202 = await resp202.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectPresentationWithDetailsResponseDto>>>();

                body201!.Data!.Items.Should().ContainSingle(x => x.ProjectPresentation.Id == scenario.InscriptionAId);
                var item201 = body201.Data.Items.Single();
                item201.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                item201.Teachers!.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                item201.Teachers!.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                body202!.Data!.Items.Should().BeEmpty();
            }

            // 5. TeachingAssignment/ByTeacher
            using (var req201 = CreateRequest(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacher/{Teacher201Id}", Teacher201Id, PermissionCodes.ProyectoGrado.N4PGPPC))
            using (var req202 = CreateRequest(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacher/{Teacher202Id}", Teacher202Id, PermissionCodes.ProyectoGrado.N4PGPPC))
            {
                var resp201 = await client.SendAsync(req201);
                var resp202 = await client.SendAsync(req202);

                resp201.StatusCode.Should().Be(HttpStatusCode.OK);
                resp202.StatusCode.Should().Be(HttpStatusCode.OK);

                var body201 = await resp201.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
                var body202 = await resp202.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();

                body201!.Data.Should().ContainSingle();
                body201.Data!.Single().Assignments.Should().ContainSingle(a => a.IdInscriptionModality == scenario.InscriptionAId);
                body202!.Data.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task Teacher201_CrossTeacherByTeacher_AndReviewOnlyGetAll_ReturnForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            await SeedScenarioAsync(factory);

            // Anti-IDOR en TeachingAssignment/ByTeacher/{idTeacher}
            using var crossTeacherReq = CreateRequest(
                HttpMethod.Get,
                $"/api/TeachingAssignment/ByTeacher/{Teacher202Id}",
                Teacher201Id,
                PermissionCodes.ProyectoGrado.N4PGPPC);
            var crossTeacherResp = await client.SendAsync(crossTeacherReq);
            crossTeacherResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Docente con solo permisos de revisión/consulta recibe 403 en /GetAll de las 4 fases
            var reviewPermissions = new[]
            {
                PermissionCodes.ProyectoGrado.N4PGPPC,
                PermissionCodes.ProyectoGrado.N4PGPAR,
                PermissionCodes.ProyectoGrado.N4PGPPRR,
                PermissionCodes.ProyectoGrado.N4PGPSR
            };

            foreach (var endpoint in new[]
            {
                "/api/Proposal/GetAll?PageNumber=1&PageSize=10",
                "/api/PreliminaryProject/GetAll?PageNumber=1&PageSize=10",
                "/api/ProjectFinal/GetAll?PageNumber=1&PageSize=10",
                "/api/ProjectPresentation/GetAll?PageNumber=1&PageSize=10"
            })
            {
                using var req = CreateRequest(HttpMethod.Get, endpoint, Teacher201Id, reviewPermissions);
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Fact]
        public async Task Committee_GetAll_OnAllFourPhases_ReturnsBothInscriptionsWithStudentsAndConfirmedTeachers()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            var committeePermissions = new[]
            {
                PermissionCodes.ProyectoGrado.N4PGPPG,
                PermissionCodes.ProyectoGrado.N4PGPAG,
                PermissionCodes.ProyectoGrado.N4PGPPRG,
                PermissionCodes.ProyectoGrado.N4PGPSG
            };

            // Proposal/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/Proposal/GetAll?PageNumber=1&PageSize=10", 1, committeePermissions))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>>>();
                var items = body!.Data!.Items.ToList();
                items.Should().HaveCount(2);

                var itemA = items.Single(x => x.Proposal.Id == scenario.InscriptionAId);
                itemA.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                itemA.Teachers.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                itemA.Teachers.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                var itemB = items.Single(x => x.Proposal.Id == scenario.InscriptionBId);
                itemB.Students.Should().ContainSingle(s => s.IdUser == Student102Id);
                itemB.Teachers.Should().ContainSingle(t => t.IdTeacher == Teacher203Id && t.IsConfirmed);
            }

            // PreliminaryProject/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/PreliminaryProject/GetAll?PageNumber=1&PageSize=10", 1, committeePermissions))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>>();
                var items = body!.Data!.Items.ToList();
                items.Should().HaveCount(2);

                var itemA = items.Single(x => x.PreliminaryProject.Id == scenario.InscriptionAId);
                itemA.Proposal.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                itemA.Proposal.Teachers.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                itemA.Proposal.Teachers.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                var itemB = items.Single(x => x.PreliminaryProject.Id == scenario.InscriptionBId);
                itemB.Proposal.Students.Should().ContainSingle(s => s.IdUser == Student102Id);
                itemB.Proposal.Teachers.Should().ContainSingle(t => t.IdTeacher == Teacher203Id && t.IsConfirmed);
            }

            // ProjectFinal/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/ProjectFinal/GetAll?PageNumber=1&PageSize=10", 1, committeePermissions))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectFinalWithDetailsResponseDto>>>();
                var items = body!.Data!.Items.ToList();
                items.Should().HaveCount(2);

                var itemA = items.Single(x => x.ProjectFinal.Id == scenario.InscriptionAId);
                itemA.Proposal.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                itemA.Proposal.Teachers.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                itemA.Proposal.Teachers.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                var itemB = items.Single(x => x.ProjectFinal.Id == scenario.InscriptionBId);
                itemB.Proposal.Students.Should().ContainSingle(s => s.IdUser == Student102Id);
                itemB.Proposal.Teachers.Should().ContainSingle(t => t.IdTeacher == Teacher203Id && t.IsConfirmed);
            }

            // ProjectPresentation/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/ProjectPresentation/GetAll?PageNumber=1&PageSize=10", 1, committeePermissions))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProjectPresentationWithDetailsResponseDto>>>();
                var items = body!.Data!.Items.ToList();
                items.Should().HaveCount(2);

                var itemA = items.Single(x => x.ProjectPresentation.Id == scenario.InscriptionAId);
                itemA.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
                itemA.Teachers!.Where(t => t.IsConfirmed).Should().ContainSingle(t => t.IdTeacher == Teacher201Id);
                itemA.Teachers!.Where(t => t.IsConfirmed).Should().NotContain(t => t.IdTeacher == Teacher202Id);

                var itemB = items.Single(x => x.ProjectPresentation.Id == scenario.InscriptionBId);
                itemB.Students.Should().ContainSingle(s => s.IdUser == Student102Id);
                itemB.Teachers.Should().ContainSingle(t => t.IdTeacher == Teacher203Id && t.IsConfirmed);
            }
        }

        [Fact]
        public async Task Committee_ByInscriptionAndPutTeachingAssignment_ShowsUnconfirmedAndRatifiesTeacher202()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            var assignPermissions = new[]
            {
                PermissionCodes.ProyectoGrado.N4PGPPA,
                PermissionCodes.ProyectoGrado.N4PGPAA
            };

            // ByInscription muestra tanto confirmado (201) como preseleccionado (202)
            using (var byInscReq = CreateRequest(
                HttpMethod.Get,
                $"/api/TeachingAssignment/ByInscription/{scenario.InscriptionAId}",
                1,
                assignPermissions))
            {
                var byInscResp = await client.SendAsync(byInscReq);
                byInscResp.StatusCode.Should().Be(HttpStatusCode.OK);

                var byInscBody = await byInscResp.Content.ReadFromJsonAsync<ApiResponse<List<TeachingAssignmentTeacherDto>>>();
                byInscBody!.Data.Should().HaveCount(2);
                byInscBody.Data!.Should().ContainSingle(t => t.IdUser == Teacher201Id && t.IsConfirmed);
                byInscBody.Data!.Should().ContainSingle(t => t.IdUser == Teacher202Id && !t.IsConfirmed);
            }

            // PUT /api/TeachingAssignment/{id} ratifica a 202 (IsConfirmed pasa a true)
            using (var ratifyReq = CreateRequest(
                HttpMethod.Put,
                $"/api/TeachingAssignment/{scenario.Assignment202Id}",
                1,
                assignPermissions))
            {
                ratifyReq.Content = JsonContent.Create(new TeachingAssignmentDto
                {
                    Id = scenario.Assignment202Id,
                    IdInscriptionModality = scenario.InscriptionAId,
                    IdTeacher = Teacher202Id,
                    IdTypeTeachingAssignment = scenario.DirectorCargoId,
                    IsConfirmed = true,
                    StatusRegister = true
                });

                var ratifyResp = await client.SendAsync(ratifyReq);
                ratifyResp.StatusCode.Should().Be(HttpStatusCode.OK);

                var ratifyBody = await ratifyResp.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
                ratifyBody!.Data.Should().NotBeNull();
                ratifyBody.Data!.Id.Should().Be(scenario.Assignment202Id);
                ratifyBody.Data.IsConfirmed.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Student101_ByUserEndpoints_OnAllFourPhases_ReturnExactInscriptionAData()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            var studentPerms = new[]
            {
                PermissionCodes.ProyectoGrado.N3PGRP,
                PermissionCodes.ProyectoGrado.N3PGRA,
                PermissionCodes.ProyectoGrado.N3PGRPR,
                PermissionCodes.ProyectoGrado.N3PGCSU
            };

            using (var req = CreateRequest(HttpMethod.Get, $"/api/Proposal/ByUser/{Student101Id}", Student101Id, studentPerms))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ProposalWithDetailsResponseDto>>>();
                body!.Data.Should().ContainSingle(p => p.Proposal.Id == scenario.InscriptionAId);
                body.Data!.Single().Students.Should().ContainSingle(s => s.IdUser == Student101Id);
            }

            using (var req = CreateRequest(HttpMethod.Get, $"/api/PreliminaryProject/ByUser/{Student101Id}", Student101Id, studentPerms))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<PreliminaryProjectWithDetailsResponseDto>>>();
                body!.Data.Should().ContainSingle(p => p.PreliminaryProject.Id == scenario.InscriptionAId);
                body.Data!.Single().Proposal.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
            }

            using (var req = CreateRequest(HttpMethod.Get, $"/api/ProjectFinal/ByUser/{Student101Id}", Student101Id, studentPerms))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ProjectFinalWithDetailsResponseDto>>>();
                body!.Data.Should().ContainSingle(p => p.ProjectFinal.Id == scenario.InscriptionAId);
                body.Data!.Single().Proposal.Students.Should().ContainSingle(s => s.IdUser == Student101Id);
            }

            using (var req = CreateRequest(HttpMethod.Get, $"/api/ProjectPresentation/ByUser/{Student101Id}", Student101Id, studentPerms))
            {
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ProjectPresentationWithDetailsResponseDto>>>();
                body!.Data.Should().ContainSingle(p => p.ProjectPresentation.Id == scenario.InscriptionAId);
                body.Data!.Single().Students.Should().ContainSingle(s => s.IdUser == Student101Id);
            }
        }

        [Fact]
        public async Task Student_ProposalSubsanacion_PutWhenNoPertinente_OwnerSucceedsAndPreservesObservation_NonMemberGetsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory, StateStageCodes.PropNoPertinente);

            var payload = new ProposalDto
            {
                Id = scenario.InscriptionAId,
                Title = "Propuesta A Subsanada",
                Description = "Descripción corregida",
                Observation = "Intento de borrar observación",
                GeneralObjective = "Objetivo corregido",
                SpecificObjectives = new List<string> { "Obj 1 corregido" },
                IdResearchLine = scenario.ResearchLineId,
                IdResearchSubLine = scenario.ResearchSubLineId,
                IdStateStage = scenario.PropRadicadaStateId,
                StatusRegister = true
            };

            // Estudiante ajeno 102 obtiene 403 Forbidden
            using (var nonMemberReq = CreateRequest(
                HttpMethod.Put,
                $"/api/Proposal/{scenario.InscriptionAId}",
                Student102Id,
                PermissionCodes.ProyectoGrado.N3PGRP))
            {
                nonMemberReq.Content = JsonContent.Create(payload);
                var nonMemberResp = await client.SendAsync(nonMemberReq);
                nonMemberResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // Estudiante dueño 101 obtiene 200 OK, estado vuelve a PROP_RADICADA y conserva Observation del comité
            using (var ownerReq = CreateRequest(
                HttpMethod.Put,
                $"/api/Proposal/{scenario.InscriptionAId}",
                Student101Id,
                PermissionCodes.ProyectoGrado.N3PGRP))
            {
                ownerReq.Content = JsonContent.Create(payload);
                var ownerResp = await client.SendAsync(ownerReq);
                ownerResp.StatusCode.Should().Be(HttpStatusCode.OK);

                var body = await ownerResp.Content.ReadFromJsonAsync<ApiResponse<ProposalDto>>();
                body!.Data.Should().NotBeNull();
                body.Data!.IdStateStage.Should().Be(scenario.PropRadicadaStateId);
                body.Data.Observation.Should().Be(scenario.ProposalAObservation);
                body.Data.Title.Should().Be("Propuesta A Subsanada");
            }
        }

        [Fact]
        public async Task Student_SuggestTeachingAssignment_OwnerDirectorUnconfirmed_JuradoBadRequest_NonMemberDenied()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            // 1. Estudiante dueño 101 sugiere DIRECTOR habilitado -> 201 Created con IsConfirmed == false
            using (var suggestDirectorReq = CreateRequest(
                HttpMethod.Post,
                "/api/TeachingAssignment",
                Student101Id,
                PermissionCodes.ProyectoGrado.N3PGRP))
            {
                suggestDirectorReq.Content = JsonContent.Create(new TeachingAssignmentDto
                {
                    IdInscriptionModality = scenario.InscriptionAId,
                    IdTeacher = Teacher203Id,
                    IdTypeTeachingAssignment = scenario.DirectorCargoId,
                    IsConfirmed = true, // intenta falsificar confirmación; el backend fuerza false
                    StatusRegister = true
                });

                var resp = await client.SendAsync(suggestDirectorReq);
                resp.StatusCode.Should().Be(HttpStatusCode.Created);

                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
                body!.Data.Should().NotBeNull();
                body.Data!.IdTeacher.Should().Be(Teacher203Id);
                body.Data.IsConfirmed.Should().BeFalse();
            }

            // 2. Estudiante dueño 101 intenta sugerir JURADO_EVALUADOR -> 400 BadRequest
            using (var suggestJuradoReq = CreateRequest(
                HttpMethod.Post,
                "/api/TeachingAssignment",
                Student101Id,
                PermissionCodes.ProyectoGrado.N3PGRP))
            {
                suggestJuradoReq.Content = JsonContent.Create(new TeachingAssignmentDto
                {
                    IdInscriptionModality = scenario.InscriptionAId,
                    IdTeacher = Teacher203Id,
                    IdTypeTeachingAssignment = scenario.JuradoCargoId,
                    IsConfirmed = false,
                    StatusRegister = true
                });

                var resp = await client.SendAsync(suggestJuradoReq);
                resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }

            // 3. Estudiante ajeno 102 intenta sugerir docente en Inscripción A -> denegado (401/403)
            using (var nonMemberReq = CreateRequest(
                HttpMethod.Post,
                "/api/TeachingAssignment",
                Student102Id,
                PermissionCodes.ProyectoGrado.N3PGRP))
            {
                nonMemberReq.Content = JsonContent.Create(new TeachingAssignmentDto
                {
                    IdInscriptionModality = scenario.InscriptionAId,
                    IdTeacher = Teacher203Id,
                    IdTypeTeachingAssignment = scenario.DirectorCargoId,
                    IsConfirmed = false,
                    StatusRegister = true
                });

                var resp = await client.SendAsync(nonMemberReq);
                resp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
            }
        }

        [Fact]
        public async Task Proposal_Patch_RequiresCommitteePermission_N4PGPPR_AndRejectsStudent_N3PGRP()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            int propPertinenteId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                propPertinenteId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropPertinente).Id;
            }

            var patchDto = new ProposalPatchDto
            {
                IdStateStage = propPertinenteId,
                Observation = "Evaluación comité PG"
            };

            // 1. OwnerStudent (101, N3PGRP) -> 403 Forbidden
            using (var ownerReq = CreateRequest(HttpMethod.Patch, $"/api/Proposal/{scenario.InscriptionAId}", Student101Id, PermissionCodes.ProyectoGrado.N3PGRP))
            {
                ownerReq.Content = JsonContent.Create(patchDto);
                var resp = await client.SendAsync(ownerReq);
                resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 2. OtherStudent (102, N3PGRP) -> 403 Forbidden
            using (var otherReq = CreateRequest(HttpMethod.Patch, $"/api/Proposal/{scenario.InscriptionAId}", Student102Id, PermissionCodes.ProyectoGrado.N3PGRP))
            {
                otherReq.Content = JsonContent.Create(patchDto);
                var resp = await client.SendAsync(otherReq);
                resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 3. CommitteePg (305, N4PGPPR) -> 200 OK y persiste en BD
            using (var committeeReq = CreateRequest(HttpMethod.Patch, $"/api/Proposal/{scenario.InscriptionAId}", 305, PermissionCodes.ProyectoGrado.N4PGPPR))
            {
                committeeReq.Content = JsonContent.Create(patchDto);
                var resp = await client.SendAsync(committeeReq);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);

                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<ProposalDto>>();
                body.Should().NotBeNull();
                body!.Success.Should().BeTrue();
                body.Data.Should().NotBeNull();
                body.Data!.IdStateStage.Should().Be(propPertinenteId);
                body.Data.Observation.Should().Be("Evaluación comité PG");
            }

            using (var verifyScope = factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await verifyContext.Set<Proposal>().FindAsync(scenario.InscriptionAId);
                persisted.Should().NotBeNull();
                persisted!.IdStateStage.Should().Be(propPertinenteId);
                persisted.Observation.Should().Be("Evaluación comité PG");
            }
        }

        [Fact]
        public async Task Proposal_Put_StudentSubsanationFromNoPertinente_TransitionsToRadicadaAndPreservesObservation()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory, StateStageCodes.PropNoPertinente);

            int propPertinenteId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                propPertinenteId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropPertinente).Id;
            }

            // OwnerStudent intenta cambiar IdStateStage a PROP_PERTINENTE y borrar Observation -> backend fuerza PROP_RADICADA y preserva Observation
            var subsanationDto = new ProposalDto
            {
                Id = scenario.InscriptionAId,
                Title = "Propuesta A Subsanada Matriz",
                Description = "Descripción subsanada",
                Observation = "",
                GeneralObjective = "Objetivo subsanado",
                SpecificObjectives = new List<string> { "Obj subsanado" },
                IdResearchLine = scenario.ResearchLineId,
                IdResearchSubLine = scenario.ResearchSubLineId,
                IdStateStage = propPertinenteId,
                StatusRegister = true
            };

            using (var req = CreateRequest(HttpMethod.Put, $"/api/Proposal/{scenario.InscriptionAId}", Student101Id, PermissionCodes.ProyectoGrado.N3PGRP))
            {
                req.Content = JsonContent.Create(subsanationDto);
                var resp = await client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);

                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<ProposalDto>>();
                body.Should().NotBeNull();
                body!.Success.Should().BeTrue();
                body.Data.Should().NotBeNull();
                body.Data!.IdStateStage.Should().Be(scenario.PropRadicadaStateId);
                body.Data.Observation.Should().Be(scenario.ProposalAObservation);
            }

            using (var verifyScope = factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await verifyContext.Set<Proposal>().FindAsync(scenario.InscriptionAId);
                persisted.Should().NotBeNull();
                persisted!.IdStateStage.Should().Be(scenario.PropRadicadaStateId);
                persisted.Observation.Should().Be(scenario.ProposalAObservation);
            }
        }

        [Fact]
        public async Task TeachingAssignment_Put_ReassignToTeacherWithoutEnabledCargo_Returns400BadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            int teacherWithoutCargoId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var idType = context.Set<IdentificationType>().First();
                var program = context.Set<AcademicProgram>().First();

                var teacherWithoutCargo = new User
                {
                    FirstName = "Docente",
                    LastName = "SinCargo",
                    Email = $"doc_sincargo_{Guid.NewGuid():N}@unicesar.edu.co",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(teacherWithoutCargo);
                await context.SaveChangesAsync();
                teacherWithoutCargoId = teacherWithoutCargo.Id;
            }

            var reassignDto = new TeachingAssignmentDto
            {
                Id = scenario.Assignment202Id,
                IdInscriptionModality = scenario.InscriptionAId,
                IdTeacher = teacherWithoutCargoId,
                IdTypeTeachingAssignment = scenario.DirectorCargoId,
                IsConfirmed = true,
                StatusRegister = true
            };

            using var req = CreateRequest(
                HttpMethod.Put,
                $"/api/TeachingAssignment/{scenario.Assignment202Id}",
                305,
                PermissionCodes.ProyectoGrado.N4PGPPA);
            req.Content = JsonContent.Create(reassignDto);

            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var body = await resp.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            body.Should().NotBeNull();
            body!.Success.Should().BeFalse();

            using (var verifyScope = factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await verifyContext.Set<TeachingAssignment>().FindAsync(scenario.Assignment202Id);
                persisted.Should().NotBeNull();
                persisted!.IdTeacher.Should().Be(Teacher202Id);
            }
        }

        [Fact]
        public async Task PreliminaryProject_TwoConfirmedEvaluatorsRequiredForEnEvaluacion_Matrix()
        {
            var storageMock = new Mock<IFileStorageService>();
            storageMock
                .Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("anteproyecto_subsanado.pdf");

            using var baseFactory = new CustomWebApplicationFactory();
            using var factory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton(_ => storageMock.Object);
                });
            });
            using var client = factory.CreateClient();
            var scenario = await SeedScenarioAsync(factory);

            int anteproyectoDocTypeId;
            int apRadicadoPendAsigEvalId;
            int apEnEvaluacionId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var apStage = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseAnteproyecto);

                var apConObservaciones = context.Set<StateStage>().FirstOrDefault(s => s.Code == StateStageCodes.ApConObservaciones);
                if (apConObservaciones == null)
                {
                    apConObservaciones = new StateStage
                    {
                        Code = StateStageCodes.ApConObservaciones,
                        Name = "Anteproyecto con Observaciones",
                        IdStageModality = apStage.Id,
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    };
                    context.Set<StateStage>().Add(apConObservaciones);
                    await context.SaveChangesAsync();
                }

                apRadicadoPendAsigEvalId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval).Id;
                apEnEvaluacionId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApEnEvaluacion).Id;
                anteproyectoDocTypeId = context.Set<DocumentType>().First(dt => dt.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;

                var preliminary = await context.Set<PreliminaryProject>().FindAsync(scenario.InscriptionAId);
                preliminary!.IdStateStage = apConObservaciones.Id;

                // Exactamente 1 JURADO_EVALUADOR confirmado previo
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = scenario.InscriptionAId,
                    IdTeacher = Teacher201Id,
                    IdTypeTeachingAssignment = scenario.JuradoCargoId,
                    IsConfirmed = true,
                    RevocationDate = null,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // 1. OwnerStudent sube entregable de Anteproyecto desde AP_CON_OBSERVACIONES con solo 1 jurado -> AP_RADICADO_PEND_ASIG_EVAL
            var uploadContent = new MultipartFormDataContent();
            uploadContent.Add(new StringContent(scenario.InscriptionAId.ToString()), "IdInscriptionModality");
            uploadContent.Add(new StringContent(anteproyectoDocTypeId.ToString()), "IdDocumentType");
            uploadContent.Add(new StringContent("Anteproyecto Subsanado v2"), "Name");
            uploadContent.Add(new StringContent(Student101Id.ToString()), "IdUserCreatedAt");
            uploadContent.Add(new StringContent("Subsanacion"), "OperationRegister");
            var pdfContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            pdfContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            uploadContent.Add(pdfContent, "File", "anteproyecto_v2.pdf");

            using (var uploadReq = CreateRequest(HttpMethod.Post, "/api/Document", Student101Id, PermissionCodes.ProyectoGrado.N3PGRA))
            {
                uploadReq.Content = uploadContent;
                var uploadResp = await client.SendAsync(uploadReq);
                uploadResp.StatusCode.Should().Be(HttpStatusCode.Created);

                var uploadBody = await uploadResp.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
                uploadBody.Should().NotBeNull();
                uploadBody!.Success.Should().BeTrue();
            }

            using (var verifyScope = factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var preliminary = await verifyContext.Set<PreliminaryProject>().FindAsync(scenario.InscriptionAId);
                preliminary.Should().NotBeNull();
                preliminary!.IdStateStage.Should().Be(apRadicadoPendAsigEvalId);
            }

            // 2. CommitteePg asigna 2do JURADO_EVALUADOR distinto confirmado -> 201 Created y avanza a AP_EN_EVALUACION
            using (var assignReq = CreateRequest(HttpMethod.Post, "/api/TeachingAssignment", 305, PermissionCodes.ProyectoGrado.N4PGPAA))
            {
                assignReq.Content = JsonContent.Create(new TeachingAssignmentDto
                {
                    IdInscriptionModality = scenario.InscriptionAId,
                    IdTeacher = Teacher202Id,
                    IdTypeTeachingAssignment = scenario.JuradoCargoId,
                    IsConfirmed = true,
                    StatusRegister = true
                });

                var assignResp = await client.SendAsync(assignReq);
                assignResp.StatusCode.Should().Be(HttpStatusCode.Created);

                var assignBody = await assignResp.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
                assignBody.Should().NotBeNull();
                assignBody!.Success.Should().BeTrue();
                assignBody.Data!.IsConfirmed.Should().BeTrue();
            }

            using (var verifyScope = factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var preliminary = await verifyContext.Set<PreliminaryProject>().FindAsync(scenario.InscriptionAId);
                preliminary.Should().NotBeNull();
                preliminary!.IdStateStage.Should().Be(apEnEvaluacionId);
            }
        }
    }
}
