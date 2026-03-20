using Application.Features.Research.EventHandlers;
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
    public class StartProjectPhaseOnPreliminaryApprovalHandlerTests : IntegrationTestBase
    {
        public StartProjectPhaseOnPreliminaryApprovalHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Handle_PreliminaryApproved_AdvancesPhaseAndCreatesProjectFinal()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Seed global catalogs
            SeedingUtilities.SeedCatalogs(context);
            SeedingUtilities.SeedPermissions(context, PermissionCodes.ProyectoGrado.N2PGPR, PermissionCodes.ProyectoGrado.N3PGRPR, PermissionCodes.ProyectoGrado.N3PGCPR);

            // 2. Setup User and Inscription in ANTEPROYECTO phase
            var user = new User
            {
                Id = 1,
                Email = "student@test.edu.co",
                FirstName = "Student",
                LastName = "Test",
                Identification = "12345",
                StatusRegister = true,
                OperationRegister = "Test setup"
            };
            context.Set<User>().Add(user);

            var pgModalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var anteproyectoStageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseAnteproyecto).Id;
            
            var inscription = new InscriptionModality
            {
                Id = 100, // Explicit ID
                IdModality = pgModalityId,
                IdStageModality = anteproyectoStageId,
                IdAcademicPeriod = 1,
                IdStateInscription = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test setup",
                StatusRegister = true
            };
            context.Set<InscriptionModality>().Add(inscription);

            var userInscription = new UserInscriptionModality
            {
                Id = 1,
                IdUser = user.Id,
                IdInscriptionModality = inscription.Id,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test setup",
                StatusRegister = true
            };
            context.Set<UserInscriptionModality>().Add(userInscription);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // 3. Setup Handler
            var handler = new StartProjectPhaseOnPreliminaryApprovalHandler(
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<StartProjectPhaseOnPreliminaryApprovalHandler>>()
            );

            // 4. Create Event: PreliminaryProject state changed to "AP_APROBADO"
            var aprobadoStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApAprobado).Id;
            var domainEvent = new PreliminaryProjectStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: pgModalityId,
                NewStateStageId: aprobadoStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();
            
            // Re-resolve context to get fresh data
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // Assert
            // 1. Verify Inscription phase advanced to Proyecto/Informe
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription.Should().NotBeNull();
            var proyectoStageId = actContext.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseProyectoInforme).Id;
            updatedInscription!.IdStageModality.Should().Be(proyectoStageId);

            // 2. Verify ProjectFinal was created
            var projectFinal = await actContext.Set<ProjectFinal>().FindAsync(inscription.Id);
            projectFinal.Should().NotBeNull();
            var pfPendienteInformeStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfPendienteInforme).Id;
            projectFinal!.IdStateStage.Should().Be(pfPendienteInformeStateId);
            projectFinal.IdUserCreatedAt.Should().Be(user.Id);

            // 3. Verify Permissions assigned
            var assignedPermissions = actContext.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Select(up => up.IdPermission)
                .ToList();
            
            var expectedPermissions = actContext.Set<Permission>()
                .Where(p => p.Code == PermissionCodes.ProyectoGrado.N2PGPR || 
                            p.Code == PermissionCodes.ProyectoGrado.N3PGRPR || 
                            p.Code == PermissionCodes.ProyectoGrado.N3PGCPR)
                .Select(p => p.Id)
                .ToList();

            assignedPermissions.Should().Contain(expectedPermissions);
        }
    }
}
