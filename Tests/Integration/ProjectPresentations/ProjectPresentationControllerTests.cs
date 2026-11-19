using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.DTOs.ProjectPresentations;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.ProjectPresentations
{
    public class ProjectPresentationControllerTests : GenericControllerIntegrationTests<ProjectPresentation, ProjectPresentationDto>
    {
        protected override string BaseUrl => "/api/ProjectPresentation";

        public ProjectPresentationControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override ProjectPresentationDto CreateValidDto()
        {
            return new ProjectPresentationDto
            {
                AttemptNumber = 1,
                Observations = "Test Observations",
                ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(7),
                PresentationModality = "PRESENCIAL",
                LocationOrLink = "Auditorio Bloque H"
            };
        }

        protected override ProjectPresentation CreateValidEntity()
        {
            return new ProjectPresentation
            {
                AttemptNumber = 1,
                Observations = "Test Observations",
                ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(7),
                PresentationModality = "PRESENCIAL",
                LocationOrLink = "Auditorio Bloque H"
            };
        }

        protected override void SeedAdditionalData(ProjectPresentation entity)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var id = SeedSustentacion(context);
            var presentation = context.Set<ProjectPresentation>().Find(id);
            if (presentation != null)
            {
                context.Set<ProjectPresentation>().Remove(presentation);
                context.SaveChanges();
            }

            var stateStage = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustPendienteProgramacion);
            entity.Id = id;
            entity.IdStateStage = stateStage.Id;
        }

        public override async Task GetAll_ReturnsOkAndList()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<ProjectPresentation>().Add(entity);
                await context.SaveChangesAsync();
            }

            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Domain.Common.PaginatedResult<ProjectPresentationWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            var response = await _client.GetAsync($"{BaseUrl}/1");
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            var response = await _client.PostAsJsonAsync(BaseUrl, CreateValidDto());
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/1", CreateValidDto());
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public override async Task UpdateStatus_ReturnsOk()
        {
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/status", new Application.Shared.DTOs.UpdateStatusRequestDto
            {
                StatusRegister = false,
                IdUserUpdateAt = 1,
                OperationRegister = "TEST_UPDATE"
            });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private int SeedSustentacion(AppDbContext context, string stateStageCode = StateStageCodes.SustPendienteProgramacion, int attemptNumber = 1)
        {
            SeedingUtilities.SeedCatalogs(context);

            var modality = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado);
            if (!context.Set<AcademicPeriod>().Any())
            {
                context.Set<AcademicPeriod>().Add(new AcademicPeriod
                {
                    Id = 1,
                    Code = "2026-1",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(6),
                    StatusRegister = true,
                    OperationRegister = "Seed"
                });
                context.SaveChanges();
            }
            var period = context.Set<AcademicPeriod>().First();
            var approvedInscriptionState = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado);
            var sustStage = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion);

            var inscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdAcademicPeriod = period.Id,
                IdStateInscription = approvedInscriptionState.Id,
                IdStageModality = sustStage.Id,
                IsFinalRejection = false,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.SaveChanges();

            var stateStage = context.Set<StateStage>().First(s => s.Code == stateStageCode);

            var rLine = context.Set<ResearchLine>().FirstOrDefault()
                ?? context.Set<ResearchLine>().Add(new ResearchLine { Name = "Line " + Guid.NewGuid(), Description = "Desc" }).Entity;
            context.SaveChanges();
            var subLine = context.Set<ResearchSubLine>().FirstOrDefault(s => s.IdResearchLine == rLine.Id)
                ?? context.Set<ResearchSubLine>().Add(new ResearchSubLine { Name = "SubLine " + Guid.NewGuid(), Description = "Desc", IdResearchLine = rLine.Id }).Entity;
            var propState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PgPropuestaInicial);
            context.SaveChanges();

            var proposal = new Proposal
            {
                Id = inscription.Id,
                Title = "Proyecto Test Sustentación",
                GeneralObjective = "Obj",
                SpecificObjectives = new List<string> { "Obj" },
                IdResearchLine = rLine.Id,
                IdResearchSubLine = subLine.Id,
                IdStateStage = propState.Id,
                StatusRegister = true
            };
            context.Set<Proposal>().Add(proposal);

            var pfState = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfInformeAprobado);
            var projectFinal = new ProjectFinal
            {
                Id = inscription.Id,
                IdStateStage = pfState.Id,
                ReportApprovalDate = DateTimeOffset.UtcNow,
                StatusRegister = true
            };
            context.Set<ProjectFinal>().Add(projectFinal);

            var presentation = new ProjectPresentation
            {
                Id = inscription.Id,
                IdStateStage = stateStage.Id,
                AttemptNumber = attemptNumber,
                ScheduledDateTime = stateStageCode != StateStageCodes.SustPendienteProgramacion ? DateTimeOffset.UtcNow.AddDays(3) : null,
                PresentationModality = stateStageCode != StateStageCodes.SustPendienteProgramacion ? "PRESENCIAL" : null,
                LocationOrLink = stateStageCode != StateStageCodes.SustPendienteProgramacion ? "Sala 101" : null,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Set<ProjectPresentation>().Add(presentation);
            context.SaveChanges();

            return inscription.Id;
        }

        private static void SeedConfirmedEvaluatorAssignment(AppDbContext context, int inscriptionId, int teacherId = 1)
        {
            var juryType = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR")
                ?? context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment
                {
                    Code = "JURADO_EVALUADOR",
                    Name = "Jurado Evaluador",
                    StatusRegister = true,
                    OperationRegister = "Seed"
                }).Entity;
            context.SaveChanges();

            context.Set<TeachingAssignment>().Add(new TeachingAssignment
            {
                IdInscriptionModality = inscriptionId,
                IdTeacher = teacherId,
                IdTypeTeachingAssignment = juryType.Id,
                IsConfirmed = true,
                RevocationDate = null,
                StatusRegister = true,
                OperationRegister = "Seed"
            });
            context.SaveChanges();
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 1. Programación de sustentación (SUST_PENDIENTE_PROGRAMACION -> SUST_PROGRAMADA)
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Patch_ProgramarSustentacion_WhenValidData_TransitionsToSustProgramada()
        {
            // Arrange
            int presentationId;
            int programadaStateId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustPendienteProgramacion);
                programadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada).Id;
            }

            var scheduledDate = DateTimeOffset.UtcNow.AddDays(5);
            var patchDto = new ProjectPresentationPatchDto
            {
                IdStateStage = programadaStateId,
                ScheduledDateTime = scheduledDate,
                PresentationModality = "PRESENCIAL",
                LocationOrLink = "Auditorio Julio Villazón",
                Observations = "Programación formal de sustentación"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", patchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectPresentationDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.IdStateStage.Should().Be(programadaStateId);
            result.Data.PresentationModality.Should().Be("PRESENCIAL");
            result.Data.LocationOrLink.Should().Be("Auditorio Julio Villazón");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var entity = context.Set<ProjectPresentation>().Find(presentationId);
                entity.Should().NotBeNull();
                entity!.IdStateStage.Should().Be(programadaStateId);
                entity.ScheduledDateTime.Should().NotBeNull();
            }
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 2. Sustentación no aprobada con reintento -> reprogramación -> bloqueo en 2do reintento
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Patch_SustentacionNoAprobadaConReintento_AndReprogramar_IncrementsAttempt_AndSecondFailureBlocks()
        {
            // Arrange
            int presentationId;
            int programadaStateId;
            int reintentoStateId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustProgramada, attemptNumber: 1);
                SeedConfirmedEvaluatorAssignment(context, presentationId);
                programadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada).Id;
                reintentoStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustNoAprobadaReintento).Id;
            }

            // Act 1: Primer fallo -> SUST_NO_APROBADA_REINTENTO
            var failDto1 = new ProjectPresentationPatchDto
            {
                IdStateStage = reintentoStateId,
                Observations = "Se detectaron falencias metodológicas. Se otorga segunda oportunidad según Art. 17."
            };
            var failResponse1 = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", failDto1);
            failResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var entity = context.Set<ProjectPresentation>().Find(presentationId);
                entity!.AttemptNumber.Should().Be(1);
                entity.ActualDateTime.Should().NotBeNull();
            }

            // Act 2: Reprogramar segunda sustentación -> SUST_PROGRAMADA
            var reprogramarDto = new ProjectPresentationPatchDto
            {
                IdStateStage = programadaStateId,
                ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(15),
                PresentationModality = "VIRTUAL",
                LocationOrLink = "https://meet.google.com/xyz-test"
            };
            var reprogramarResponse = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", reprogramarDto);
            reprogramarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var entity = context.Set<ProjectPresentation>().Find(presentationId);
                entity!.AttemptNumber.Should().Be(2, "al reprogramar tras SUST_NO_APROBADA_REINTENTO se incrementa AttemptNumber a 2");
            }

            // Act 3: Intentar segundo reintento con AttemptNumber == 2 -> Debe bloquearse
            var failDto2 = new ProjectPresentationPatchDto
            {
                IdStateStage = reintentoStateId,
                Observations = "Segundo intento no satisfactorio"
            };
            var failResponse2 = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", failDto2);

            // Assert: Bloqueo según Art. 17
            failResponse2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 3. Sustentación aprobada -> estampa ActualDateTime y ProjectFinal.FinalPhaseApprovalDate y bloquea modificaciones
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Patch_SustentacionAprobada_StampsDates_AndBlocksSubsequentModification()
        {
            // Arrange
            int presentationId;
            int aprobadaStateId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustProgramada);
                SeedConfirmedEvaluatorAssignment(context, presentationId);
                aprobadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustAprobada).Id;
            }

            var actualDate = DateTimeOffset.UtcNow;
            var patchDto = new ProjectPresentationPatchDto
            {
                IdStateStage = aprobadaStateId,
                ActualDateTime = actualDate,
                Observations = "Sustentación aprobada por unanimidad con calificación excelente."
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", patchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var presentation = context.Set<ProjectPresentation>().Find(presentationId);
                presentation.Should().NotBeNull();
                presentation!.IdStateStage.Should().Be(aprobadaStateId);
                presentation.ActualDateTime.Should().NotBeNull();

                var projectFinal = context.Set<ProjectFinal>().Find(presentationId);
                projectFinal.Should().NotBeNull();
                projectFinal!.FinalPhaseApprovalDate.Should().NotBeNull(
                    "al aprobar la sustentación se debe estampar FinalPhaseApprovalDate en ProjectFinal");
            }

            // Intento posterior de modificar estado de registro final -> Bloqueado (400 BadRequest)
            var modifyDto = new ProjectPresentationPatchDto
            {
                Observations = "Intento de cambiar observaciones tras aprobación"
            };
            var modifyResponse = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", modifyDto);
            modifyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 4. Sustentación reprobada definitiva -> marca InscriptionModality.IsFinalRejection = true y bloquea modificaciones
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Patch_SustentacionReprobadaFinal_SetsIsFinalRejection_AndBlocksSubsequentModification()
        {
            // Arrange
            int presentationId;
            int reprobadaStateId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustProgramada);
                SeedConfirmedEvaluatorAssignment(context, presentationId);
                reprobadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustReprobadaFinal).Id;
            }

            var patchDto = new ProjectPresentationPatchDto
            {
                IdStateStage = reprobadaStateId,
                Observations = "Reprobada definitivamente según dictamen del jurado."
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", patchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var presentation = context.Set<ProjectPresentation>().Find(presentationId);
                presentation!.IdStateStage.Should().Be(reprobadaStateId);

                var inscription = context.Set<InscriptionModality>().Find(presentationId);
                inscription.Should().NotBeNull();
                inscription!.IsFinalRejection.Should().BeTrue(
                    "al pasar a SUST_REPROBADA_FINAL debe marcarse InscriptionModality.IsFinalRejection = true");
            }

            // Intento posterior de modificar estado de registro rechazado -> Bloqueado (400 BadRequest)
            var modifyDto = new ProjectPresentationPatchDto
            {
                Observations = "Intento de modificar tras reprobación final"
            };
            var modifyResponse = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", modifyDto);
            modifyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 5. GetByTeacherId debe incluir Teachers asignados (Jurado / Director)
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task GetByTeacherId_ReturnsAssignedTeachers()
        {
            // Arrange
            int presentationId;
            int teacherId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustProgramada);

                var juryType = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR")
                    ?? context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment
                    {
                        Code = "JURADO_EVALUADOR",
                        Name = "Jurado Evaluador",
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    }).Entity;

                var teacher = new User
                {
                    Email = "jurado.test@unicesar.edu.co",
                    FirstName = "Carlos",
                    LastName = "Jurado",
                    Identification = "99887766",
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<User>().Add(teacher);
                context.SaveChanges();

                teacherId = teacher.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = presentationId,
                    IdTeacher = teacherId,
                    IdTypeTeachingAssignment = juryType.Id,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                });
                context.SaveChanges();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByTeacher/{teacherId}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Domain.Common.PaginatedResult<ProjectPresentationWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            var item = result.Data!.Items.FirstOrDefault(i => i.ProjectPresentation.Id == presentationId);
            item.Should().NotBeNull();
            item!.Teachers.Should().NotBeNullOrEmpty();
            item.Teachers!.Should().Contain(t => t.IdTeacher == teacherId && t.CargoCode == "JURADO_EVALUADOR");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // 6. Patch con misma observación previa no debe duplicar registros en Evaluation
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Patch_WithUnchangedObservations_DoesNotDuplicateEvaluationRow()
        {
            // Arrange
            int presentationId;
            int programadaStateId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustPendienteProgramacion);
                programadaStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada).Id;

                if (!context.Set<EvaluationType>().Any(et => et.Code == EvaluationTypeCodes.ProjectPresentation))
                {
                    context.Set<EvaluationType>().Add(new EvaluationType
                    {
                        Code = EvaluationTypeCodes.ProjectPresentation,
                        Name = "Evaluación Sustentación",
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    });
                    context.SaveChanges();
                }
            }

            const string obs = "Programación inicial de sustentación";
            var firstPatch = new ProjectPresentationPatchDto
            {
                IdStateStage = programadaStateId,
                ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(5),
                PresentationModality = "PRESENCIAL",
                LocationOrLink = "Auditorio Principal",
                Observations = obs
            };
            var resp1 = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", firstPatch);
            resp1.StatusCode.Should().Be(HttpStatusCode.OK);

            // Reenviar PATCH sin cambiar Observations (solo cambia lugar)
            var secondPatch = new ProjectPresentationPatchDto
            {
                LocationOrLink = "Sala de Juntas B",
                Observations = obs
            };
            var resp2 = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", secondPatch);
            resp2.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var evals = context.Set<Evaluation>()
                    .Where(e => e.EntityType == nameof(ProjectPresentation) && e.EntityId == presentationId)
                    .ToList();
                evals.Should().HaveCount(1, "no debe duplicarse Evaluation si Observations no cambió");
            }
        }

        [Fact]
        public async Task Patch_WithInvalidPresentationModality_ReturnsBadRequest()
        {
            int presentationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                presentationId = SeedSustentacion(context, StateStageCodes.SustPendienteProgramacion);
            }

            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{presentationId}", new ProjectPresentationPatchDto
            {
                PresentationModality = "TELEPATIA"
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private async Task<(int InscriptionId, int StudentId, int TeacherId, int ManagerId, int AdminId)> SeedProjectFinalReadyForSustentacionAsync(AppDbContext context)
        {
            SeedingUtilities.SeedCatalogs(context);

            var permN1 = await context.Set<Permission>().FirstOrDefaultAsync(p => p.Code == PermissionCodes.ProyectoGrado.N1PG)
                ?? context.Set<Permission>().Add(new Permission { Code = PermissionCodes.ProyectoGrado.N1PG, Description = "PG", StatusRegister = true }).Entity;
            var permN2 = await context.Set<Permission>().FirstOrDefaultAsync(p => p.Code == PermissionCodes.ProyectoGrado.N2PGSU)
                ?? context.Set<Permission>().Add(new Permission { Code = PermissionCodes.ProyectoGrado.N2PGSU, ParentCode = PermissionCodes.ProyectoGrado.N1PG, Description = "Sust", StatusRegister = true }).Entity;
            var permN3 = await context.Set<Permission>().FirstOrDefaultAsync(p => p.Code == PermissionCodes.ProyectoGrado.N3PGCSU)
                ?? context.Set<Permission>().Add(new Permission { Code = PermissionCodes.ProyectoGrado.N3PGCSU, ParentCode = PermissionCodes.ProyectoGrado.N2PGSU, Description = "Consultar Sust", StatusRegister = true }).Entity;

            if (!await context.Set<EvaluationType>().AnyAsync(et => et.Code == EvaluationTypeCodes.ProjectFinal))
                context.Set<EvaluationType>().Add(new EvaluationType { Code = EvaluationTypeCodes.ProjectFinal, Name = "Eval PF", StatusRegister = true });
            if (!await context.Set<EvaluationType>().AnyAsync(et => et.Code == EvaluationTypeCodes.ProjectPresentation))
                context.Set<EvaluationType>().Add(new EvaluationType { Code = EvaluationTypeCodes.ProjectPresentation, Name = "Eval Sust", StatusRegister = true });

            var idType = await context.Set<IdentificationType>().FirstOrDefaultAsync()
                ?? context.Set<IdentificationType>().Add(new IdentificationType { Code = "CC", Name = "Cedula", Description = "CC", StatusRegister = true }).Entity;
            var program = await context.Set<AcademicProgram>().FirstOrDefaultAsync()
                ?? context.Set<AcademicProgram>().Add(new AcademicProgram { Code = "SYS", Name = "Sistemas", StatusRegister = true }).Entity;
            var period = await context.Set<AcademicPeriod>().FirstOrDefaultAsync()
                ?? context.Set<AcademicPeriod>().Add(new AcademicPeriod { Code = "2026-2", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddMonths(6), StatusRegister = true }).Entity;
            var rLine = await context.Set<ResearchLine>().FirstOrDefaultAsync()
                ?? context.Set<ResearchLine>().Add(new ResearchLine { Name = "Linea E2E", Description = "Desc", StatusRegister = true }).Entity;
            await context.SaveChangesAsync();

            var subLine = await context.Set<ResearchSubLine>().FirstOrDefaultAsync(s => s.IdResearchLine == rLine.Id)
                ?? context.Set<ResearchSubLine>().Add(new ResearchSubLine { Name = "SubLinea E2E", Description = "Desc", IdResearchLine = rLine.Id, StatusRegister = true }).Entity;

            var student = new User { FirstName = "Est", LastName = "Sust", Email = $"est.{Guid.NewGuid():N}@unicesar.edu.co", Identification = Guid.NewGuid().ToString("N")[..10], IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            var teacher = new User { FirstName = "Jur", LastName = "Sust", Email = $"jur.{Guid.NewGuid():N}@unicesar.edu.co", Identification = Guid.NewGuid().ToString("N")[..10], IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            var manager = new User { FirstName = "Gestor", LastName = "Sust", Email = $"ges.{Guid.NewGuid():N}@unicesar.edu.co", Identification = Guid.NewGuid().ToString("N")[..10], IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            var admin = new User { FirstName = "Admin", LastName = "Sust", Email = $"adm.{Guid.NewGuid():N}@unicesar.edu.co", Identification = Guid.NewGuid().ToString("N")[..10], IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            context.Set<User>().AddRange(student, teacher, manager, admin);

            var juryType = await context.Set<TypeTeachingAssignment>().FirstOrDefaultAsync(t => t.Code == "JURADO_EVALUADOR")
                ?? context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment { Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", StatusRegister = true }).Entity;
            await context.SaveChangesAsync();

            var pgModality = await context.Set<Modality>().FirstAsync(m => m.Code == ModalityCodes.ProyectoGrado);
            var approvedState = await context.Set<StateInscription>().FirstAsync(s => s.Code == StateInscriptionCodes.Aprobado);
            var pfStage = await context.Set<StageModality>().FirstAsync(s => s.Code == StageModalityCodes.PgFaseProyectoInforme);
            var pfEnEvalState = await context.Set<StateStage>().FirstAsync(s => s.Code == StateStageCodes.PfinfRadicadoEnEvaluacion);
            var propInitState = await context.Set<StateStage>().FirstAsync(s => s.Code == StateStageCodes.PgPropuestaInicial);

            var inscription = new InscriptionModality
            {
                IdModality = pgModality.Id,
                IdAcademicPeriod = period.Id,
                IdStateInscription = approvedState.Id,
                IdStageModality = pfStage.Id,
                IsFinalRejection = false,
                StatusRegister = true,
                OperationRegister = "SeedE2E",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdInscriptionModality = inscription.Id,
                IdUser = student.Id,
                IsConfirmed = true,
                StatusRegister = true
            });
            context.Set<Proposal>().Add(new Proposal
            {
                Id = inscription.Id,
                Title = "Proyecto E2E Sustentacion",
                GeneralObjective = "Obj",
                SpecificObjectives = ["Obj 1"],
                IdResearchLine = rLine.Id,
                IdResearchSubLine = subLine.Id,
                IdStateStage = propInitState.Id,
                StatusRegister = true
            });
            context.Set<ProjectFinal>().Add(new ProjectFinal
            {
                Id = inscription.Id,
                IdStateStage = pfEnEvalState.Id,
                StatusRegister = true
            });
            context.Set<TeachingAssignment>().Add(new TeachingAssignment
            {
                IdInscriptionModality = inscription.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = juryType.Id,
                IsConfirmed = true,
                RevocationDate = null,
                StatusRegister = true
            });
            await context.SaveChangesAsync();

            return (inscription.Id, student.Id, teacher.Id, manager.Id, admin.Id);
        }

        private static HttpRequestMessage CreateAuthorizedRequest(
            HttpMethod method,
            string url,
            int userId,
            string permissions,
            object body)
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("X-Test-UserId", userId.ToString());
            request.Headers.Add("X-Test-Permissions", permissions);
            return request;
        }

        [Fact]
        public async Task EndToEndHttpFlow_ProjectFinalApprovedToSustentacion_RetryAndAttempt2Approved_WithPermissionAndValidationGuards()
        {
            using var seedContext = GetFreshDbContext();
            var ids = await SeedProjectFinalReadyForSustentacionAsync(seedContext);

            var pfAprobadoId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfInformeAprobado).Id;
            var sustStageId = seedContext.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion).Id;
            var sustPendienteId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustPendienteProgramacion).Id;
            var sustProgramadaId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada).Id;
            var sustReintentoId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustNoAprobadaReintento).Id;
            var sustAprobadaId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustAprobada).Id;

            // 1. PATCH /api/ProjectFinal/{id} -> PFINF_INFORME_APROBADO dispara UnitOfWork -> AdvanceProjectFinalToSustentacionHandler
            using (var pfPatchReq = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectFinal/{ids.InscriptionId}",
                ids.TeacherId,
                PermissionCodes.ProyectoGrado.N4PGPPRR,
                new ProjectFinalPatchDto
                {
                    IdStateStage = pfAprobadoId,
                    Observations = "Informe final aprobado por jurado"
                }))
            {
                var pfPatchRes = await _client.SendAsync(pfPatchReq);
                pfPatchRes.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var verifyScope = _factory.Services.CreateScope())
            {
                var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pf = await verifyDb.Set<ProjectFinal>().FindAsync(ids.InscriptionId);
                pf!.ReportApprovalDate.Should().NotBeNull();

                var insc = await verifyDb.Set<InscriptionModality>().FindAsync(ids.InscriptionId);
                insc!.IdStageModality.Should().Be(sustStageId);

                var presentation = await verifyDb.Set<ProjectPresentation>().FindAsync(ids.InscriptionId);
                presentation.Should().NotBeNull();
                presentation!.Id.Should().Be(ids.InscriptionId);
                presentation.AttemptNumber.Should().Be(1);
                presentation.IdStateStage.Should().Be(sustPendienteId);

                var studentPerms = await verifyDb.Set<UserPermission>()
                    .Where(up => up.IdUser == ids.StudentId && up.StatusRegister)
                    .Join(verifyDb.Set<Permission>(), up => up.IdPermission, p => p.Id, (_, p) => p.Code)
                    .ToListAsync();
                studentPerms.Should().Contain([PermissionCodes.ProyectoGrado.N2PGSU, PermissionCodes.ProyectoGrado.N3PGCSU]);
            }

            // 2. Actores sin N4PGPSG ni N4PGPSR reciben 403 Forbidden en PATCH /api/ProjectPresentation/{id}
            foreach (var (actorId, perms) in new[]
            {
                (ids.StudentId, $"{PermissionCodes.ProyectoGrado.N3PGCSU},{PermissionCodes.ProyectoGrado.N3PGRPR}"),
                (ids.AdminId, PermissionCodes.Configuraciones.N3CFSU),
                (ids.TeacherId, $"{PermissionCodes.ProyectoGrado.N4PGPPRR},{PermissionCodes.ProyectoGrado.N4PGPAR}")
            })
            {
                using var forbiddenReq = CreateAuthorizedRequest(
                    HttpMethod.Patch,
                    $"/api/ProjectPresentation/{ids.InscriptionId}",
                    actorId,
                    perms,
                    new ProjectPresentationPatchDto { Observations = "Sin permiso de sustentación" });
                (await _client.SendAsync(forbiddenReq)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 3. Programación con campos obligatorios faltantes -> 400 BadRequest
            foreach (var invalidSchedule in new[]
            {
                new ProjectPresentationPatchDto { IdStateStage = sustProgramadaId, PresentationModality = "PRESENCIAL", LocationOrLink = "Sala 1" },
                new ProjectPresentationPatchDto { IdStateStage = sustProgramadaId, ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(3), LocationOrLink = "Sala 1" },
                new ProjectPresentationPatchDto { IdStateStage = sustProgramadaId, ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(3), PresentationModality = "PRESENCIAL" }
            })
            {
                using var invalidReq = CreateAuthorizedRequest(
                    HttpMethod.Patch,
                    $"/api/ProjectPresentation/{ids.InscriptionId}",
                    ids.ManagerId,
                    PermissionCodes.ProyectoGrado.N4PGPSG,
                    invalidSchedule);
                (await _client.SendAsync(invalidReq)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }

            // 4. Programación válida (N4PGPSG -> SUST_PROGRAMADA)
            using (var scheduleReq = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.ManagerId,
                PermissionCodes.ProyectoGrado.N4PGPSG,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustProgramadaId,
                    ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(5),
                    PresentationModality = "PRESENCIAL",
                    LocationOrLink = "Auditorio Principal"
                }))
            {
                var scheduleRes = await _client.SendAsync(scheduleReq);
                scheduleRes.StatusCode.Should().Be(HttpStatusCode.OK);
                var payload = await scheduleRes.Content.ReadFromJsonAsync<ApiResponse<ProjectPresentationDto>>();
                payload!.Data!.AttemptNumber.Should().Be(1);
                payload.Data.IdStateStage.Should().Be(sustProgramadaId);
            }

            // 5. Intento 1 no aprobado con reintento (N4PGPSR -> SUST_NO_APROBADA_REINTENTO)
            using (var attempt1Req = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.TeacherId,
                PermissionCodes.ProyectoGrado.N4PGPSR,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustReintentoId,
                    Observations = "Requiere segunda sustentación"
                }))
            {
                var attempt1Res = await _client.SendAsync(attempt1Req);
                attempt1Res.StatusCode.Should().Be(HttpStatusCode.OK);
                var payload = await attempt1Res.Content.ReadFromJsonAsync<ApiResponse<ProjectPresentationDto>>();
                payload!.Data!.AttemptNumber.Should().Be(1);
                payload.Data.ActualDateTime.Should().NotBeNull();
            }

            // 6. Reprogramación (N4PGPSG -> SUST_PROGRAMADA incrementa AttemptNumber a 2)
            using (var rescheduleReq = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.ManagerId,
                PermissionCodes.ProyectoGrado.N4PGPSG,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustProgramadaId,
                    ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(15),
                    PresentationModality = "VIRTUAL",
                    LocationOrLink = "https://meet.example/retry"
                }))
            {
                var rescheduleRes = await _client.SendAsync(rescheduleReq);
                rescheduleRes.StatusCode.Should().Be(HttpStatusCode.OK);
                var payload = await rescheduleRes.Content.ReadFromJsonAsync<ApiResponse<ProjectPresentationDto>>();
                payload!.Data!.AttemptNumber.Should().Be(2);
            }

            // 7. Intento 2 aprobado (N4PGPSR -> SUST_APROBADA)
            using (var attempt2Req = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.TeacherId,
                PermissionCodes.ProyectoGrado.N4PGPSR,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustAprobadaId,
                    Observations = "Aprobada en segundo intento"
                }))
            {
                var attempt2Res = await _client.SendAsync(attempt2Req);
                attempt2Res.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var finalScope = _factory.Services.CreateScope())
            {
                var finalDb = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pres = await finalDb.Set<ProjectPresentation>().FindAsync(ids.InscriptionId);
                pres!.IdStateStage.Should().Be(sustAprobadaId);
                pres.AttemptNumber.Should().Be(2);

                var pf = await finalDb.Set<ProjectFinal>().FindAsync(ids.InscriptionId);
                pf!.FinalPhaseApprovalDate.Should().NotBeNull();

                var insc = await finalDb.Set<InscriptionModality>().FindAsync(ids.InscriptionId);
                insc!.IsFinalRejection.Should().BeFalse();
            }
        }

        [Fact]
        public async Task EndToEndHttpFlow_ProjectFinalApprovedToSustentacion_RetryAndAttempt2ReprobadaFinal()
        {
            using var seedContext = GetFreshDbContext();
            var ids = await SeedProjectFinalReadyForSustentacionAsync(seedContext);

            var pfAprobadoId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfInformeAprobado).Id;
            var sustProgramadaId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustProgramada).Id;
            var sustReintentoId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustNoAprobadaReintento).Id;
            var sustReprobadaFinalId = seedContext.Set<StateStage>().First(s => s.Code == StateStageCodes.SustReprobadaFinal).Id;

            // 1. Aprobar informe final -> crea ProjectPresentation en SUST_PENDIENTE_PROGRAMACION
            using (var pfPatchReq = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectFinal/{ids.InscriptionId}",
                ids.TeacherId,
                PermissionCodes.ProyectoGrado.N4PGPPRR,
                new ProjectFinalPatchDto
                {
                    IdStateStage = pfAprobadoId,
                    Observations = "Informe final aprobado"
                }))
            {
                (await _client.SendAsync(pfPatchReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 2. Programar intento 1
            using (var schedule1Req = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.ManagerId,
                PermissionCodes.ProyectoGrado.N4PGPSG,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustProgramadaId,
                    ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(4),
                    PresentationModality = "MIXTA",
                    LocationOrLink = "Sala Mixta 201"
                }))
            {
                (await _client.SendAsync(schedule1Req)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 3. Intento 1 no aprobado con reintento
            using (var attempt1Req = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.TeacherId,
                PermissionCodes.ProyectoGrado.N4PGPSR,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustReintentoId,
                    Observations = "No aprobada en primer intento"
                }))
            {
                (await _client.SendAsync(attempt1Req)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 4. Reprogramar intento 2
            using (var schedule2Req = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.ManagerId,
                PermissionCodes.ProyectoGrado.N4PGPSG,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustProgramadaId,
                    ScheduledDateTime = DateTimeOffset.UtcNow.AddDays(12),
                    PresentationModality = "PRESENCIAL",
                    LocationOrLink = "Auditorio B"
                }))
            {
                (await _client.SendAsync(schedule2Req)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 5. Intento 2 reprobada definitiva -> marca IsFinalRejection = true
            using (var attempt2Req = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/ProjectPresentation/{ids.InscriptionId}",
                ids.TeacherId,
                PermissionCodes.ProyectoGrado.N4PGPSR,
                new ProjectPresentationPatchDto
                {
                    IdStateStage = sustReprobadaFinalId,
                    Observations = "Reprobada definitivamente en segundo intento"
                }))
            {
                var attempt2Res = await _client.SendAsync(attempt2Req);
                attempt2Res.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pres = await verifyDb.Set<ProjectPresentation>().FindAsync(ids.InscriptionId);
            pres!.IdStateStage.Should().Be(sustReprobadaFinalId);
            pres.AttemptNumber.Should().Be(2);

            var insc = await verifyDb.Set<InscriptionModality>().FindAsync(ids.InscriptionId);
            insc!.IsFinalRejection.Should().BeTrue();
        }
    }
}
