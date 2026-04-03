using Application.Features.Research.EventHandlers;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
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
            // Seed de catálogos una sola vez para todas las pruebas de esta clase.
            // La BD en memoria compartida persiste entre las pruebas de la misma colección.
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!context.Set<DocumentType>().Any())
                SeedingUtilities.SeedCatalogs(context);
        }

        private AdvancePhaseOnDocumentUploadedHandler CreateHandler() =>
            new(_scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AdvancePhaseOnDocumentUploadedHandler>>(),
                _scope.ServiceProvider.GetRequiredService<IJobEnqueuer>());

        // ──────────────────────────────────────────────────────────────────────────────
        // Escenario 1: Subir AnteproyectoEntregable mientras está en AP_PENDIENTE_DOCUMENTO
        //             → avanza PreliminaryProject a AP_RADICADO_PEND_ASIG_EVAL
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

            // Crear PreliminaryProject en AP_PENDIENTE_DOCUMENTO
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
                ChangedByUserId: user.Id
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
        // Escenario 2: Subir AnteproyectoEntregable cuando NO está en AP_PENDIENTE_DOCUMENTO
        //             → NO debe avanzar (cláusula guard)
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_DoesNotAdvance_WhenNotInPendienteDocumento()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Estados ya seedeados por la prueba anterior (BD en memoria compartida), solo referenciarlos
            var radicadoStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval).Id;

            var user = new User
            {
                Id = 2, Email = "student2@test.edu.co", FirstName = "C", LastName = "D",
                Identification = "22", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            // Crear PreliminaryProject ya en estado RADICADO (no PENDIENTE_DOC)
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
                ChangedByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - el estado debe permanecer RADICADO (sin cambios)
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);

            updatedProject.Should().NotBeNull();
            updatedProject!.IdStateStage.Should().Be(radicadoStateId,
                "el handler no debe avanzar el estado si el proyecto ya no está en AP_PENDIENTE_DOCUMENTO");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Escenario 3: Subir ProyectoFinalEntregable mientras está en PFINF_PENDIENTE_INFORME
        //             → avanza ProjectFinal a PFINF_RADICADO_EN_EVALUACION
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
                ChangedByUserId: user.Id
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
