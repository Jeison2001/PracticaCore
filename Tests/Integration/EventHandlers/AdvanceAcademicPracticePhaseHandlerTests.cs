using Application.Features.AcademicPractices.EventHandlers;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.EventHandlers
{
    public class AdvanceAcademicPracticePhaseHandlerTests : IntegrationTestBase
    {
        public AdvanceAcademicPracticePhaseHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!context.Set<Modality>().Any())
                SeedingUtilities.SeedCatalogs(context);
        }

        private AdvanceAcademicPracticePhaseHandler CreateHandler() =>
            new(_scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AdvanceAcademicPracticePhaseHandler>>());

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 1: PaInscripcionAprobada → advances to PaFaseDesarrollo + F1 perms
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_InscripcionAprobada_AdvancesToDesarrolloAndAssignsF1Permissions()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedPermissions(context,
                PermissionCodes.PracticaAcademica.N2PAF1,
                PermissionCodes.PracticaAcademica.N3PAF1R,
                PermissionCodes.PracticaAcademica.N3PAF1C);

            var user = new User
            {
                Id = 1, Email = "pa1@test.edu.co", FirstName = "A", LastName = "B",
                Identification = "100", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var paModalityId    = context.Set<Modality>().First(m => m.Code == ModalityCodes.PracticaAcademica).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;
            var inscFaseId      = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseInscripcion).Id;

            var inscription = new InscriptionModality
            {
                Id = 1, IdModality = paModalityId, IdStageModality = inscFaseId,
                IdStateInscription = aprobadoStateId, IdAcademicPeriod = 1,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                Id = 1, IdUser = user.Id, IdInscriptionModality = inscription.Id,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            });

            // Seed AcademicPractice for date stamping (handler updates it on state transitions)
            var academicPractice = new AcademicPractice
            {
                Id = inscription.Id,
                Title = "Test Practice",
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<AcademicPractice>().Add(academicPractice);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var newStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PaInscripcionAprobada).Id;
            var oldStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PaInscripcionPendDoc).Id;

            var domainEvent = new AcademicPracticeStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: paModalityId,
                NewStateStageId: newStateId,
                OldStateStageId: oldStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);

            updatedInscription.Should().NotBeNull();
            var desarrolloStageId = actContext.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseDesarrollo).Id;
            updatedInscription!.IdStageModality.Should().Be(desarrolloStageId,
                "al aprobar la inscripción de PA debe avanzar a Fase Desarrollo");

            var f1Perms = actContext.Set<Permission>()
                .Where(p => p.Code == PermissionCodes.PracticaAcademica.N2PAF1 ||
                            p.Code == PermissionCodes.PracticaAcademica.N3PAF1R ||
                            p.Code == PermissionCodes.PracticaAcademica.N3PAF1C)
                .Select(p => p.Id).ToList();

            var assigned = actContext.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Select(up => up.IdPermission).ToList();

            assigned.Should().Contain(f1Perms, "los permisos F1 deben asignarse al aprobar inscripción PA");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 2: PaDesarrolloAprobada → advances to PaFaseEvaluacion + F2 perms
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_DesarrolloAprobado_AdvancesToEvaluacionAndAssignsF2Permissions()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedPermissions(context,
                PermissionCodes.PracticaAcademica.N2PAF2,
                PermissionCodes.PracticaAcademica.N3PAF2R,
                PermissionCodes.PracticaAcademica.N3PAF2C);

            var user = new User
            {
                Id = 2, Email = "pa2@test.edu.co", FirstName = "C", LastName = "D",
                Identification = "200", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var paModalityId    = context.Set<Modality>().First(m => m.Code == ModalityCodes.PracticaAcademica).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;
            var devFaseId       = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseDesarrollo).Id;

            var inscription = new InscriptionModality
            {
                Id = 2, IdModality = paModalityId, IdStageModality = devFaseId,
                IdStateInscription = aprobadoStateId, IdAcademicPeriod = 1,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                Id = 2, IdUser = user.Id, IdInscriptionModality = inscription.Id,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            });

            // Seed AcademicPractice for date stamping (handler updates it on state transitions)
            var academicPractice = new AcademicPractice
            {
                Id = inscription.Id,
                Title = "Test Practice",
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<AcademicPractice>().Add(academicPractice);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var newStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PaDesarrolloAprobada).Id;
            var oldStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PaInscripcionAprobada).Id;

            var domainEvent = new AcademicPracticeStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: paModalityId,
                NewStateStageId: newStateId,
                OldStateStageId: oldStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);

            updatedInscription.Should().NotBeNull();
            var evalStageId = actContext.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseEvaluacion).Id;
            updatedInscription!.IdStageModality.Should().Be(evalStageId,
                "al aprobar el desarrollo de PA debe avanzar a Fase Evaluación");

            var f2Perms = actContext.Set<Permission>()
                .Where(p => p.Code == PermissionCodes.PracticaAcademica.N2PAF2 ||
                            p.Code == PermissionCodes.PracticaAcademica.N3PAF2R ||
                            p.Code == PermissionCodes.PracticaAcademica.N3PAF2C)
                .Select(p => p.Id).ToList();

            var assigned = actContext.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Select(up => up.IdPermission).ToList();

            assigned.Should().Contain(f2Perms, "los permisos F2 deben asignarse al aprobar el desarrollo PA");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 3: Guard: same old/new state → no advance
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_SameOldAndNewState_DoesNothing()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var paModalityId    = context.Set<Modality>().First(m => m.Code == ModalityCodes.PracticaAcademica).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;
            var inscFaseId      = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseInscripcion).Id;

            var user = new User
            {
                Id = 3, Email = "pa3@test.edu.co", FirstName = "E", LastName = "F",
                Identification = "300", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var inscription = new InscriptionModality
            {
                Id = 3, IdModality = paModalityId, IdStageModality = inscFaseId,
                IdStateInscription = aprobadoStateId, IdAcademicPeriod = 1,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var sameStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PaInscripcionAprobada).Id;

            var domainEvent = new AcademicPracticeStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: paModalityId,
                NewStateStageId: sameStateId,
                OldStateStageId: sameStateId, // same → guard fires immediately
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - phase should remain in PaFaseInscripcion
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription!.IdStageModality.Should().Be(inscFaseId,
                "si OldStateId == NewStateId, el handler no debe hacer nada");
        }
    }
}
