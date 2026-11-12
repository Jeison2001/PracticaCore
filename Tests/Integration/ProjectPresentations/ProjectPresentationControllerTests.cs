using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.ProjectPresentations;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
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
            entity.IdInscriptionModality = id;
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

        public override Task GetById_ReturnsOkAndEntity()
        {
            return Task.CompletedTask;
        }

        public override async Task Create_ReturnsCreated()
        {
            var dto = CreateValidDto();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var id = SeedSustentacion(context);
                var presentation = context.Set<ProjectPresentation>().Find(id);
                if (presentation != null)
                {
                    context.Set<ProjectPresentation>().Remove(presentation);
                    context.SaveChanges();
                }

                var stateStage = context.Set<StateStage>().First(s => s.Code == StateStageCodes.SustPendienteProgramacion);
                dto.Id = id;
                dto.IdInscriptionModality = id;
                dto.IdStateStage = stateStage.Id;
            }

            var response = await _client.PostAsJsonAsync(BaseUrl, dto);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectPresentationDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Id.Should().Be(dto.Id);
        }

        public override async Task Update_ReturnsOk()
        {
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<ProjectPresentation>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;
            updateDto.IdInscriptionModality = entity.Id;
            updateDto.IdStateStage = entity.IdStateStage;
            updateDto.Observations = "Updated Observations";

            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProjectPresentationDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Observations.Should().Be("Updated Observations");
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
                IdInscriptionModality = inscription.Id,
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
    }
}
