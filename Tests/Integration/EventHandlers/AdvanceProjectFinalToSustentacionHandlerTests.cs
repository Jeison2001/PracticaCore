using Application.Features.Research.EventHandlers;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.EventHandlers
{
    public class AdvanceProjectFinalToSustentacionHandlerTests : IntegrationTestBase
    {
        public AdvanceProjectFinalToSustentacionHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private AdvanceProjectFinalToSustentacionHandler CreateHandler(AppDbContext context) =>
            new(new UnitOfWork(context,
                _scope.ServiceProvider.GetRequiredService<IMediator>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<UnitOfWork>>()),
                _scope.ServiceProvider.GetRequiredService<ILogger<AdvanceProjectFinalToSustentacionHandler>>());

        private (InscriptionModality inscription, User user) SeedProyectoFinalEnCurso(AppDbContext context, int id, string email)
        {
            var user = new User
            {
                Id = id, Email = email, FirstName = "G", LastName = "H",
                Identification = id.ToString("000"), StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var pgModalityId   = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;
            var pfinfFaseId     = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseProyectoInforme).Id;

            var inscription = new InscriptionModality
            {
                Id = id, IdModality = pgModalityId, IdStageModality = pfinfFaseId,
                IdStateInscription = aprobadoStateId, IdAcademicPeriod = 1,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                Id = id, IdUser = user.Id, IdInscriptionModality = inscription.Id,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            });

            // ProjectFinal en curso, pendiente de informe
            var pendienteId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfPendienteInforme).Id;
            context.Set<ProjectFinal>().Add(new ProjectFinal
            {
                Id = inscription.Id,
                IdStateStage = pendienteId,
                StatusRegister = true,
                OperationRegister = "Test"
            });

            context.SaveChanges();
            context.ChangeTracker.Clear();
            return (inscription, user);
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Escenario 1: PFINF_INFORME_APROBADO → avanza a PG_FASE_SUSTENTACION + fecha
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_InformeAprobado_AdvancesToSustentacionAndStampsDate()
        {
            // Arrange - usar BD aislada FRESCA para este test
            var context = GetFreshDbContext();
            SeedingUtilities.SeedCatalogs(context);
            var (inscription, user) = SeedProyectoFinalEnCurso(context, 1, "pg1@test.edu.co");

            var newStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfInformeAprobado).Id;
            var oldStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfRadicadoEnEvaluacion).Id;

            var domainEvent = new ProjectFinalStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                NewStateStageId: newStateId,
                OldStateStageId: oldStateId,
                TriggeredByUserId: user.Id
            );

            // Act - usar el MISMO contexto (con BD fresca)
            await CreateHandler(context).Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - usar el MISMO contexto
            var updatedInscription = context.Set<InscriptionModality>().Find(inscription.Id);
            updatedInscription.Should().NotBeNull();
            var sustStageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion).Id;
            updatedInscription!.IdStageModality.Should().Be(sustStageId,
                "al aprobar el informe final el flujo de Proyecto de Grado debe avanzar a Fase Sustentación");
            updatedInscription.OperationRegister.Should().Contain("Fase Sustentación asignada por DomainEvent");

            var projectFinal = context.Set<ProjectFinal>().Find(inscription.Id);
            projectFinal.Should().NotBeNull();
            projectFinal!.ReportApprovalDate.Should().NotBeNull(
                "la aprobación del informe debe estampar ReportApprovalDate");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Escenario 2: estado distinto (radicado en evaluación) → no avanza ni estampa
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_OtherState_DoesNothing()
        {
            // Arrange - usar BD aislada FRESCA para este test
            var context = GetFreshDbContext();
            SeedingUtilities.SeedCatalogs(context);
            var (inscription, user) = SeedProyectoFinalEnCurso(context, 2, "pg2@test.edu.co");
            var pfinfFaseId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseProyectoInforme).Id;

            var newStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfRadicadoEnEvaluacion).Id;
            var oldStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfPendienteInforme).Id;

            var domainEvent = new ProjectFinalStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                NewStateStageId: newStateId,
                OldStateStageId: oldStateId,
                TriggeredByUserId: user.Id
            );

            // Act - usar el MISMO contexto (con BD fresca)
            await CreateHandler(context).Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - la fase debe permanecer en PG_FASE_PROYECTO_INFORME y sin fecha
            var updatedInscription = context.Set<InscriptionModality>().Find(inscription.Id);
            updatedInscription!.IdStageModality.Should().Be(pfinfFaseId,
                "solo PFINF_INFORME_APROBADO debe disparar el avance a Sustentación");

            var projectFinal = context.Set<ProjectFinal>().Find(inscription.Id);
            projectFinal!.ReportApprovalDate.Should().BeNull(
                "el estampado de fecha solo ocurre al aprobar el informe");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Escenario 3: idempotencia — inscripción ya en Sustentación → no repite avance
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_AlreadyInSustentacion_DoesNotDuplicateAdvance()
        {
            // Arrange - usar BD aislada FRESCA para este test
            var context = GetFreshDbContext();
            SeedingUtilities.SeedCatalogs(context);
            var (inscription, user) = SeedProyectoFinalEnCurso(context, 3, "pg3@test.edu.co");

            var sustStageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseSustentacion).Id;
            var reloaded = context.Set<InscriptionModality>().Find(inscription.Id)!;
            reloaded.IdStageModality = sustStageId;
            var operaciones = reloaded.OperationRegister;
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var newStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfInformeAprobado).Id;
            var oldStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfRadicadoEnEvaluacion).Id;

            var domainEvent = new ProjectFinalStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                NewStateStageId: newStateId,
                OldStateStageId: oldStateId,
                TriggeredByUserId: user.Id
            );

            // Act - usar el MISMO contexto (con BD fresca)
            await CreateHandler(context).Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - la inscripción sigue en Sustentación y OperationRegister no se duplica
            var updatedInscription = context.Set<InscriptionModality>().Find(inscription.Id);
            updatedInscription!.IdStageModality.Should().Be(sustStageId,
                "la inscripción ya estaba en Fase Sustentación");
            updatedInscription.OperationRegister.Should().Be(operaciones,
                "el avance no debe repetirse ni acumular el registro de operación");
        }
    }
}
