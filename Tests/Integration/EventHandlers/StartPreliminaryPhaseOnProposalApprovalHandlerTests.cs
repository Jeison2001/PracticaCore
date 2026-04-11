using Application.Features.Proposals.EventHandlers;
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
    public class StartPreliminaryPhaseOnProposalApprovalHandlerTests : IntegrationTestBase
    {
        public StartPreliminaryPhaseOnProposalApprovalHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Handle_ProposalApproved_AdvancesPhaseAndCreatesPreliminaryProject()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Seed global catalogs
            SeedingUtilities.SeedCatalogs(context);
            SeedingUtilities.SeedPermissions(context, PermissionCodes.ProyectoGrado.N2PGA, PermissionCodes.ProyectoGrado.N3PGRA, PermissionCodes.ProyectoGrado.N3PGCA);

            // 2. Setup User and Inscription in PROPOSAL phase
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
            var propuestaStageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFasePropuesta).Id;
            
            var inscription = new InscriptionModality
            {
                Id = 100, // Explicit ID
                IdModality = pgModalityId,
                IdStageModality = propuestaStageId,
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
            var handler = new StartPreliminaryPhaseOnProposalApprovalHandler(
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<StartPreliminaryPhaseOnProposalApprovalHandler>>()
            );

            // 4. Create Event: Proposal state changed to "PERTINENTE"
            var pertinenteStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PropPertinente).Id;
            var domainEvent = new ProposalStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                NewStateStageId: pertinenteStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Verify Inscription phase advanced to Anteproyecto
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription.Should().NotBeNull();
            var anteproyectoStageId = actContext.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFaseAnteproyecto).Id;
            updatedInscription!.IdStageModality.Should().Be(anteproyectoStageId);

            // 2. Verify PreliminaryProject was created
            var preliminaryProject = await actContext.Set<PreliminaryProject>().FindAsync(inscription.Id);
            preliminaryProject.Should().NotBeNull();
            var apPendienteDocStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.ApPendienteDocumento).Id;
            preliminaryProject!.IdStateStage.Should().Be(apPendienteDocStateId);
            preliminaryProject.IdUserCreatedAt.Should().Be(user.Id);

            // 3. Verify Permissions assigned
            var assignedPermissions = actContext.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Select(up => up.IdPermission)
                .ToList();
            
            var expectedPermissions = actContext.Set<Permission>()
                .Where(p => p.Code == PermissionCodes.ProyectoGrado.N2PGA || 
                            p.Code == PermissionCodes.ProyectoGrado.N3PGRA || 
                            p.Code == PermissionCodes.ProyectoGrado.N3PGCA)
                .Select(p => p.Id)
                .ToList();

            assignedPermissions.Should().Contain(expectedPermissions);
        }
    }
}
