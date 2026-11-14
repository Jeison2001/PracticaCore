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

        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_WithTwoConfirmedJurados_AdvancesToEnEvaluacion()
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pendienteDocStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApPendienteDocumento).Id;

            var juradoCargo = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR")
                ?? new TypeTeachingAssignment { Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", Description = "Jurado", MaxAssignments = 20, StatusRegister = true };
            if (juradoCargo.Id == 0)
            {
                context.Set<TypeTeachingAssignment>().Add(juradoCargo);
                await context.SaveChangesAsync();
            }

            var user = new User
            {
                Id = 10, Email = "student10@test.edu.co", FirstName = "S10", LastName = "Test",
                Identification = "1010", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator1 = new User
            {
                Id = 11, Email = "eval1@test.edu.co", FirstName = "E1", LastName = "Test",
                Identification = "1011", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator2 = new User
            {
                Id = 12, Email = "eval2@test.edu.co", FirstName = "E2", LastName = "Test",
                Identification = "1012", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().AddRange(user, evaluator1, evaluator2);

            var preliminaryProject = new PreliminaryProject
            {
                Id = 10,
                IdStateStage = pendienteDocStateId,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<PreliminaryProject>().Add(preliminaryProject);

            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator1.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                },
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator2.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;
            var domainEvent = new DocumentUploadedEvent(preliminaryProject.Id, documentTypeId, user.Id);

            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);
            var enEvaluacionStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.ApEnEvaluacion).Id;
            updatedProject!.IdStateStage.Should().Be(enEvaluacionStateId);
        }

        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_WithUnconfirmedJurado_AdvancesToRadicadoPendAsigEval()
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pendienteDocStateId = context.Set<StateStage>().First(s => s.Code == StateStageCodes.ApPendienteDocumento).Id;

            var juradoCargo = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR")
                ?? new TypeTeachingAssignment { Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", Description = "Jurado", MaxAssignments = 20, StatusRegister = true };
            if (juradoCargo.Id == 0)
            {
                context.Set<TypeTeachingAssignment>().Add(juradoCargo);
                await context.SaveChangesAsync();
            }

            var user = new User
            {
                Id = 13, Email = "student13@test.edu.co", FirstName = "S13", LastName = "Test",
                Identification = "1013", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator1 = new User
            {
                Id = 14, Email = "eval14@test.edu.co", FirstName = "E14", LastName = "Test",
                Identification = "1014", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator2 = new User
            {
                Id = 15, Email = "eval15@test.edu.co", FirstName = "E15", LastName = "Test",
                Identification = "1015", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().AddRange(user, evaluator1, evaluator2);

            var preliminaryProject = new PreliminaryProject
            {
                Id = 13,
                IdStateStage = pendienteDocStateId,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<PreliminaryProject>().Add(preliminaryProject);

            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator1.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                },
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator2.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = false,
                    StatusRegister = true
                });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;
            var domainEvent = new DocumentUploadedEvent(preliminaryProject.Id, documentTypeId, user.Id);

            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);
            var radicadoStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval).Id;
            updatedProject!.IdStateStage.Should().Be(radicadoStateId);
        }

        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_FromConObservaciones_WithLessThanTwoConfirmedJurados_AdvancesToRadicadoPendAsigEval()
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var conObsState = context.Set<StateStage>().FirstOrDefault(s => s.Code == StateStageCodes.ApConObservaciones);
            if (conObsState == null)
            {
                conObsState = new StateStage
                {
                    Code = StateStageCodes.ApConObservaciones,
                    Name = "Anteproyecto Con Observaciones",
                    IdStageModality = 5,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<StateStage>().Add(conObsState);
                await context.SaveChangesAsync();
            }

            var juradoCargo = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR")
                ?? new TypeTeachingAssignment { Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", Description = "Jurado", MaxAssignments = 20, StatusRegister = true };
            if (juradoCargo.Id == 0)
            {
                context.Set<TypeTeachingAssignment>().Add(juradoCargo);
                await context.SaveChangesAsync();
            }

            var user = new User
            {
                Id = 20, Email = "student20@test.edu.co", FirstName = "S20", LastName = "Test",
                Identification = "2020", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator1 = new User
            {
                Id = 21, Email = "eval21@test.edu.co", FirstName = "E21", LastName = "Test",
                Identification = "2021", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator2 = new User
            {
                Id = 22, Email = "eval22@test.edu.co", FirstName = "E22", LastName = "Test",
                Identification = "2022", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().AddRange(user, evaluator1, evaluator2);

            var preliminaryProject = new PreliminaryProject
            {
                Id = 20,
                IdStateStage = conObsState.Id,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<PreliminaryProject>().Add(preliminaryProject);

            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator1.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                },
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator2.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = false,
                    StatusRegister = true
                });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;
            var domainEvent = new DocumentUploadedEvent(preliminaryProject.Id, documentTypeId, user.Id);

            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);
            var radicadoStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval).Id;
            updatedProject!.IdStateStage.Should().Be(radicadoStateId);
        }

        [Fact]
        public async Task Handle_AnteproyectoDocumentUploaded_FromConObservaciones_WithTwoConfirmedJurados_AdvancesToEnEvaluacion()
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var conObsState = context.Set<StateStage>().FirstOrDefault(s => s.Code == StateStageCodes.ApConObservaciones);
            if (conObsState == null)
            {
                conObsState = new StateStage
                {
                    Code = StateStageCodes.ApConObservaciones,
                    Name = "Anteproyecto Con Observaciones",
                    IdStageModality = 5,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<StateStage>().Add(conObsState);
                await context.SaveChangesAsync();
            }

            var juradoCargo = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR")
                ?? new TypeTeachingAssignment { Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", Description = "Jurado", MaxAssignments = 20, StatusRegister = true };
            if (juradoCargo.Id == 0)
            {
                context.Set<TypeTeachingAssignment>().Add(juradoCargo);
                await context.SaveChangesAsync();
            }

            var user = new User
            {
                Id = 23, Email = "student23@test.edu.co", FirstName = "S23", LastName = "Test",
                Identification = "2023", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator1 = new User
            {
                Id = 24, Email = "eval24@test.edu.co", FirstName = "E24", LastName = "Test",
                Identification = "2024", StatusRegister = true, OperationRegister = "Test"
            };
            var evaluator2 = new User
            {
                Id = 25, Email = "eval25@test.edu.co", FirstName = "E25", LastName = "Test",
                Identification = "2025", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().AddRange(user, evaluator1, evaluator2);

            var preliminaryProject = new PreliminaryProject
            {
                Id = 23,
                IdStateStage = conObsState.Id,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<PreliminaryProject>().Add(preliminaryProject);

            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator1.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                },
                new TeachingAssignment
                {
                    IdInscriptionModality = preliminaryProject.Id,
                    IdTeacher = evaluator2.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                });

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var documentTypeId = context.Set<DocumentType>().First(d => d.Code == DocumentTypeCodes.AnteproyectoEntregable).Id;
            var domainEvent = new DocumentUploadedEvent(preliminaryProject.Id, documentTypeId, user.Id);

            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedProject = await actContext.Set<PreliminaryProject>().FindAsync(preliminaryProject.Id);
            var enEvaluacionStateId = actContext.Set<StateStage>().First(s => s.Code == StateStageCodes.ApEnEvaluacion).Id;
            updatedProject!.IdStateStage.Should().Be(enEvaluacionStateId);
        }
    }
}
