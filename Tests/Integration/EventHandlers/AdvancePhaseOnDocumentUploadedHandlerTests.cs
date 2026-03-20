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
    public class AdvancePhaseOnDocumentUploadedHandlerTests : IntegrationTestBase
    {
        public AdvancePhaseOnDocumentUploadedHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            // Seed catalogs once for all tests in this class.
            // The shared in-memory DB persists across tests within the same collection.
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!context.Set<DocumentType>().Any())
                SeedingUtilities.SeedCatalogs(context);
        }

        private AdvancePhaseOnDocumentUploadedHandler CreateHandler() =>
            new(_scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AdvancePhaseOnDocumentUploadedHandler>>());

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 1: Uploading AnteproyectoEntregable while in AP_PENDIENTE_DOCUMENTO
        //             → advances PreliminaryProject to AP_RADICADO_PEND_ASIG_EVAL
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_AdvancesPreliminaryProjectToRadicado()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = new User
            {
                Id = 1, Email = "student@test.edu.co", FirstName = "A", LastName = "B",
                Identification = "11", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            // Create PreliminaryProject in AP_PENDIENTE_DOCUMENTO
            var pendienteDocStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApPendienteDocumento).Id;
            var preliminaryProject = new PreliminaryProject
            {
                Id = 1,
                IdStateStage = pendienteDocStateId,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<PreliminaryProject>().Add(preliminaryProject);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;

            var domainEvent = new DocumentUploadedEvent(
                InscriptionModalityId: preliminaryProject.Id,
                DocumentTypeId: documentTypeId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);

            updatedProject.Should().NotBeNull();
            var radicadoStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval).Id;
            updatedProject!.IdStateStage.Should().Be(radicadoStateId,
                "al subir el entregable de anteproyecto se debe pasar a RADICADO_PEND_ASIG_EVAL");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 2: Uploading AnteproyectoEntregable while NOT in AP_PENDIENTE_DOCUMENTO
        //             → should NOT advance (guard clause)
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_DoesNotAdvance_WhenNotInPendienteDocumento()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // States already seeded by the previous test (shared in-memory DB), just reference them
            var radicadoStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval).Id;

            var user = new User
            {
                Id = 2, Email = "student2@test.edu.co", FirstName = "C", LastName = "D",
                Identification = "22", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            // Create PreliminaryProject already in RADICADO state (not PENDIENTE_DOC)
            var preliminaryProject = new PreliminaryProject
            {
                Id = 2,
                IdStateStage = radicadoStateId, // already advanced
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<PreliminaryProject>().Add(preliminaryProject);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;
            var domainEvent = new DocumentUploadedEvent(
                InscriptionModalityId: preliminaryProject.Id,
                DocumentTypeId: documentTypeId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - state should remain RADICADO (unchanged)
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);

            updatedProject.Should().NotBeNull();
            updatedProject!.IdStateStage.Should().Be(radicadoStateId,
                "el handler no debe avanzar el estado si el proyecto ya no está en AP_PENDIENTE_DOCUMENTO");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 3: Uploading ProyectoFinalEntregable while in PFINF_PENDIENTE_INFORME
        //             → advances ProjectFinal to PFINF_RADICADO_EN_EVALUACION
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_ProyectoFinalDocumentUploaded_AdvancesProjectFinalToRadicado()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendienteInformeStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfPendienteInforme).Id;

            var user = new User
            {
                Id = 3, Email = "student3@test.edu.co", FirstName = "E", LastName = "F",
                Identification = "33", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var projectFinal = new ProjectFinal
            {
                Id = 3,
                IdStateStage = pendienteInformeStateId,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<ProjectFinal>().Add(projectFinal);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.ProyectoFinalEntregable).Id;
            var domainEvent = new DocumentUploadedEvent(
                InscriptionModalityId: projectFinal.Id,
                DocumentTypeId: documentTypeId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<ProjectFinal>().FindAsync(projectFinal.Id);

            updatedProject.Should().NotBeNull();
            var radicadoEnEvaluacionStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.PfinfRadicadoEnEvaluacion).Id;
            updatedProject!.IdStateStage.Should().Be(radicadoEnEvaluacionStateId,
                "al subir el entregable de proyecto final se debe pasar a PFINF_RADICADO_EN_EVALUACION");
        }
    }
}
