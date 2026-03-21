using Application.Features.MinorModalities.EventHandlers;
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
    public class StartMinorModalityPhaseOnApprovalHandlerTests : IntegrationTestBase
    {
        public StartMinorModalityPhaseOnApprovalHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!context.Set<Modality>().Any())
                SeedingUtilities.SeedCatalogs(context);
        }

        private StartMinorModalityPhaseOnApprovalHandler CreateHandler() =>
            new(_scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<StartMinorModalityPhaseOnApprovalHandler>>());

        // Each Theory row: (userId, inscriptionId, modalityCode, requiresApproval, stateCode, permCodes[])
        public static TheoryData<int, int, string, bool, string, string[]> ModalityScenarios() => new()
        {
            // CoTerminal (RequiresApproval=false) → triggers on NO_APLICA
            { 1, 1, ModalityCodes.CoTerminal,          false, StateInscriptionCodes.NoAplica, [PermissionCodes.CoTerminal.N1CT,            PermissionCodes.CoTerminal.N2CTC] },
            // Seminario (RequiresApproval=true) → triggers on APROBADO
            { 2, 2, ModalityCodes.SeminarioAct,         true,  StateInscriptionCodes.Aprobado, [PermissionCodes.Seminario.N1SA,             PermissionCodes.Seminario.N2SAC] },
            // Publicacion Articulo (RequiresApproval=true) → triggers on APROBADO
            { 3, 3, ModalityCodes.PublicacionArticulo,  true,  StateInscriptionCodes.Aprobado, [PermissionCodes.PublicacionArticulo.N1PC,  PermissionCodes.PublicacionArticulo.N2PCC] },
            // Grado Promedio (RequiresApproval=false) → triggers on NO_APLICA
            { 4, 4, ModalityCodes.GradoPromedio,       false, StateInscriptionCodes.NoAplica, [PermissionCodes.GradoPromedio.N1GP,       PermissionCodes.GradoPromedio.N2GPES, PermissionCodes.GradoPromedio.N2GPR] },
            // Saber Pro (RequiresApproval=false) → triggers on NO_APLICA
            { 5, 5, ModalityCodes.SaberPro,            false, StateInscriptionCodes.NoAplica, [PermissionCodes.SaberPro.N1SP,            PermissionCodes.SaberPro.N2SPC] },
        };

        [Theory]
        [MemberData(nameof(ModalityScenarios))]
        public async Task Handle_MinorModalityApproved_CreatesRecordAndAssignsPermissions(
            int userId, int inscriptionId, string modalityCode, bool requiresApproval, 
            string triggerStateCode, string[] expectedPermCodes)
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedPermissions(context, expectedPermCodes);

            var modality = context.Set<Modality>().First(m => m.Code == modalityCode);
            // Update RequiresApproval to match the scenario
            if (modality.RequiresApproval != requiresApproval)
            {
                modality.RequiresApproval = requiresApproval;
                await context.SaveChangesAsync();
            }

            var triggerState = context.Set<StateInscription>().First(s => s.Code == triggerStateCode);
            var pendienteState = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Pendiente);

            var user = new User
            {
                Id = userId, Email = $"minor{userId}@test.edu.co", FirstName = "M", LastName = "M",
                Identification = userId.ToString("D3"), StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var inscription = new InscriptionModality
            {
                Id = inscriptionId, IdModality = modality.Id, 
                IdStateInscription = pendienteState.Id,
                IdAcademicPeriod = 1, CreatedAt = DateTime.UtcNow,
                StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                Id = userId, IdUser = user.Id, IdInscriptionModality = inscription.Id,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var domainEvent = new InscriptionStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: modality.Id,
                NewStateInscriptionId: triggerState.Id,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. The inscription must have a phase assigned (Phase 1 of its modality)
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription.Should().NotBeNull();
            updatedInscription!.IdStageModality.Should().NotBeNull(
                $"la modalidad {modalityCode} debe asignar la Fase 1 al activarse");

            // 2. Assigned permissions must include expected ones
            var permIds = actContext.Set<Permission>()
                .Where(p => expectedPermCodes.Contains(p.Code))
                .Select(p => p.Id).ToList();

            var assignedPermIds = actContext.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Select(up => up.IdPermission).ToList();

            assignedPermIds.Should().Contain(permIds,
                $"los permisos iniciales de {modalityCode} deben asignarse al activarse");

            // 3. The extension record must be created according to the modality
            switch (modalityCode)
            {
                case ModalityCodes.CoTerminal:
                    (await actContext.Set<CoTerminal>().FindAsync(inscription.Id)).Should().NotBeNull();
                    break;
                case ModalityCodes.SeminarioAct:
                    (await actContext.Set<Seminar>().FindAsync(inscription.Id)).Should().NotBeNull();
                    break;
                case ModalityCodes.PublicacionArticulo:
                    (await actContext.Set<ScientificArticle>().FindAsync(inscription.Id)).Should().NotBeNull();
                    break;
                case ModalityCodes.GradoPromedio:
                    (await actContext.Set<AcademicAverage>().FindAsync(inscription.Id)).Should().NotBeNull();
                    break;
                case ModalityCodes.SaberPro:
                    (await actContext.Set<SaberPro>().FindAsync(inscription.Id)).Should().NotBeNull();
                    break;
            }
        }

        [Fact]
        public async Task Handle_ProyectoGradoModality_IsIgnored()
        {
            // Arrange — PG is NOT a minor modality → handler should do nothing
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pgModalityId    = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;

            var user = new User
            {
                Id = 10, Email = "pg10@test.edu.co", FirstName = "X", LastName = "Y",
                Identification = "010", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var inscription = new InscriptionModality
            {
                Id = 10, IdModality = pgModalityId, IdStateInscription = aprobadoStateId,
                IdAcademicPeriod = 1, CreatedAt = DateTime.UtcNow,
                StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var domainEvent = new InscriptionStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: pgModalityId,
                NewStateInscriptionId: aprobadoStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - IdStageModality should remain null
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription!.IdStageModality.Should().BeNull(
                "Proyecto de Grado no es una modalidad menor y debe ser ignorado por este handler");
        }
    }
}
