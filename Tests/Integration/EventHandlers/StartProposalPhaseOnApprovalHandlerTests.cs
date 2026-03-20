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
    public class StartProposalPhaseOnApprovalHandlerTests : IntegrationTestBase
    {
        public StartProposalPhaseOnApprovalHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Handle_ProyectoGradoAprobado_ActivatesPropuestaPhaseAndAssignsPermissions()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedCatalogs(context);
            SeedingUtilities.SeedPermissions(context,
                PermissionCodes.ProyectoGrado.N1PG,
                PermissionCodes.ProyectoGrado.N2PGP,
                PermissionCodes.ProyectoGrado.N3PGRP,
                PermissionCodes.ProyectoGrado.N3PGCP);

            var user = new User
            {
                Id = 1, Email = "pg@test.edu.co", FirstName = "PG", LastName = "Student",
                Identification = "001", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var pgModalityId     = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var aprobadoStateId  = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;

            var inscription = new InscriptionModality
            {
                Id = 1, IdModality = pgModalityId, IdStateInscription = aprobadoStateId,
                IdAcademicPeriod = 1, CreatedAt = DateTime.UtcNow,
                StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                Id = 1, IdUser = user.Id, IdInscriptionModality = inscription.Id,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var handler = new StartProposalPhaseOnApprovalHandler(
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<StartProposalPhaseOnApprovalHandler>>()
            );

            var domainEvent = new InscriptionStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: pgModalityId,
                NewStateInscriptionId: aprobadoStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Inscription debe estar en Fase Propuesta
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription.Should().NotBeNull();
            var propuestaStageId = actContext.Set<StageModality>().First(s => s.Code == StageModalityCodes.PgFasePropuesta).Id;
            updatedInscription!.IdStageModality.Should().Be(propuestaStageId);

            // 2. Los 4 permisos iniciales de Proyecto Grado deben estar asignados
            var perms = actContext.Set<Permission>()
                .Where(p => p.Code == PermissionCodes.ProyectoGrado.N1PG ||
                            p.Code == PermissionCodes.ProyectoGrado.N2PGP ||
                            p.Code == PermissionCodes.ProyectoGrado.N3PGRP ||
                            p.Code == PermissionCodes.ProyectoGrado.N3PGCP)
                .Select(p => p.Id).ToList();

            var assigned = actContext.Set<UserPermission>()
                .Where(up => up.IdUser == user.Id && up.StatusRegister)
                .Select(up => up.IdPermission).ToList();

            assigned.Should().Contain(perms);
        }

        [Fact]
        public async Task Handle_NonProyectoGradoModality_DoesNothing()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var paModalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.PracticaAcademica).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;

            var user = new User
            {
                Id = 2, Email = "pa@test.edu.co", FirstName = "PA", LastName = "Student",
                Identification = "002", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var inscription = new InscriptionModality
            {
                Id = 2, IdModality = paModalityId, IdStateInscription = aprobadoStateId,
                IdAcademicPeriod = 1, CreatedAt = DateTime.UtcNow,
                StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var handler = new StartProposalPhaseOnApprovalHandler(
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<StartProposalPhaseOnApprovalHandler>>()
            );

            var domainEvent = new InscriptionStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: paModalityId,
                NewStateInscriptionId: aprobadoStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - IdStageModality should remain null (no phase assigned)
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            updatedInscription!.IdStageModality.Should().BeNull(
                "el handler solo actúa para Proyecto de Grado, no para Práctica Académica");
        }
    }
}
