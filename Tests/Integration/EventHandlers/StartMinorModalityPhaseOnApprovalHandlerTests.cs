using Application.Features.MinorModalities.EventHandlers;
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
    public class StartMinorModalityPhaseOnApprovalHandlerTests : IntegrationTestBase
    {
        public StartMinorModalityPhaseOnApprovalHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private StartMinorModalityPhaseOnApprovalHandler CreateHandler(AppDbContext context) =>
            new(new UnitOfWork(context,
                _scope.ServiceProvider.GetRequiredService<IMediator>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<UnitOfWork>>()),
                _scope.ServiceProvider.GetRequiredService<ILogger<StartMinorModalityPhaseOnApprovalHandler>>());

        // Cada fila Theory: (userId, inscriptionId, modalityCode, requiresApproval, stateCode, permCodes[])
        public static TheoryData<int, int, string, bool, string, string[]> ModalityScenarios() => new()
        {
            // CoTerminal (RequiresApproval=false) → se dispara en NO_APLICA
            { 1, 1, ModalityCodes.CoTerminal,          false, StateInscriptionCodes.NoAplica, [PermissionCodes.CoTerminal.N1CT,            PermissionCodes.CoTerminal.N2CTC] },
            // Seminario (RequiresApproval=true) → se dispara en APROBADO
            { 2, 2, ModalityCodes.SeminarioAct,         true,  StateInscriptionCodes.Aprobado, [PermissionCodes.Seminario.N1SA,             PermissionCodes.Seminario.N2SAC] },
            // Publicacion Articulo (RequiresApproval=true) → se dispara en APROBADO
            { 3, 3, ModalityCodes.PublicacionArticulo,  true,  StateInscriptionCodes.Aprobado, [PermissionCodes.PublicacionArticulo.N1PC,  PermissionCodes.PublicacionArticulo.N2PCC] },
            // Grado Promedio (RequiresApproval=false) → se dispara en NO_APLICA
            { 4, 4, ModalityCodes.GradoPromedio,       false, StateInscriptionCodes.NoAplica, [PermissionCodes.GradoPromedio.N1GP,       PermissionCodes.GradoPromedio.N2GPES, PermissionCodes.GradoPromedio.N2GPR] },
            // Saber Pro (RequiresApproval=false) → se dispara en NO_APLICA
            { 5, 5, ModalityCodes.SaberPro,            false, StateInscriptionCodes.NoAplica, [PermissionCodes.SaberPro.N1SP,            PermissionCodes.SaberPro.N2SPC] },
        };

        [Theory]
        [MemberData(nameof(ModalityScenarios))]
        public async Task Handle_MinorModalityApproved_CreatesRecordAndAssignsPermissions(
            int userId, int inscriptionId, string modalityCode, bool requiresApproval,
            string triggerStateCode, string[] expectedPermCodes)
        {
            // Arrange - usar BD aislada FRESCA para este test
            var context = GetFreshDbContext();
            SeedingUtilities.SeedCatalogs(context);
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

            // Act - usar el MISMO contexto (con BD fresca)
            await CreateHandler(context).Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - usar el MISMO contexto
            // 1. La inscripción debe tener una fase asignada (Fase 1 de su modalidad)
            var updatedInscription = context.Set<InscriptionModality>().Find(inscription.Id);
            updatedInscription.Should().NotBeNull();
            updatedInscription!.IdStageModality.Should().NotBeNull(
                $"la modalidad {modalityCode} debe asignar la Fase 1 al activarse");

            // 2. Los permisos asignados deben incluir los esperados
            var assignedPermCodes = context.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Join(context.Set<Permission>(),
                      up => up.IdPermission,
                      p => p.Id,
                      (up, p) => p.Code)
                .ToList();

            assignedPermCodes.Should().Contain(expectedPermCodes,
                $"los permisos iniciales de {modalityCode} deben asignarse al activarse");

            // 3. El registro de extensión debe crearse según la modalidad
            switch (modalityCode)
            {
                case ModalityCodes.CoTerminal:
                    context.Set<CoTerminal>().Find(inscription.Id).Should().NotBeNull();
                    break;
                case ModalityCodes.SeminarioAct:
                    context.Set<Seminar>().Find(inscription.Id).Should().NotBeNull();
                    break;
                case ModalityCodes.PublicacionArticulo:
                    context.Set<ScientificArticle>().Find(inscription.Id).Should().NotBeNull();
                    break;
                case ModalityCodes.GradoPromedio:
                    context.Set<AcademicAverage>().Find(inscription.Id).Should().NotBeNull();
                    break;
                case ModalityCodes.SaberPro:
                    context.Set<SaberPro>().Find(inscription.Id).Should().NotBeNull();
                    break;
            }
        }

        [Fact]
        public async Task Handle_ProyectoGradoModality_IsIgnored()
        {
            // Arrange — PG NO es una modalidad menor → el handler no debe hacer nada
            var context = GetFreshDbContext();
            SeedingUtilities.SeedCatalogs(context);

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

            // Act - usar el MISMO contexto
            await CreateHandler(context).Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - IdStageModality debe permanecer null
            var updatedInscription = context.Set<InscriptionModality>().Find(inscription.Id);
            updatedInscription!.IdStageModality.Should().BeNull(
                "Proyecto de Grado no es una modalidad menor y debe ser ignorado por este handler");
        }
    }
}
