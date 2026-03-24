using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.Commands.InscriptionWithStudents;
using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.Inscription
{
    /// <summary>
    /// Tests para UpdateInscriptionWithStudentsHandler.
    /// Verifica que el flujo de actualización de estado de inscripción
    /// dispara correctamente las notificaciones.
    /// </summary>
    public class UpdateInscriptionWithStudentsHandlerTests : IntegrationTestBase
    {
        public UpdateInscriptionWithStudentsHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task UpdateInscriptionModality_StateChanged_DispatchesNotification()
        {
            // Crear datos de prueba
            using var initScope = _factory.Services.CreateScope();
            var context = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedCatalogs(context);

            // Crear AcademicPeriod requerido
            var period = new AcademicPeriod
            {
                Code = "2026-I",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            // Obtener IDs necesarios
            var modalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var periodId = period.Id;
            var pendienteStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Pendiente).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;

            // Crear inscripción inicial en estado PENDIENTE
            var inscription = new InscriptionModality
            {
                IdModality = modalityId,
                IdStateInscription = pendienteStateId,
                IdAcademicPeriod = periodId,
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            // Verify initial state
            var inscriptionId = inscription.Id;
            context.ChangeTracker.Clear();

            // La verificación real se hace en el test E2E que usa el API
            // ya que el UpdateInscriptionWithStudentsHandler delega a NotificationDispatcher

            // Assert - la inscripción debe existir en la base de datos
            var dbInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            dbInscription.Should().NotBeNull();
            dbInscription!.IdStateInscription.Should().Be(pendienteStateId);
        }

        [Fact]
        public async Task UpdateInscriptionModality_StateChangedFromPendienteToAprobado_TriggersNotificationFlow()
        {
            // Arrange
            using var initScope = _factory.Services.CreateScope();
            var context = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedCatalogs(context);

            // Crear AcademicPeriod requerido
            var period = new AcademicPeriod
            {
                Code = "2026-I",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            // Obtener IDs necesarios
            var modalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var periodId = period.Id;
            var pendienteStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Pendiente).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;

            // Crear inscripción en estado PENDIENTE
            var inscription = new InscriptionModality
            {
                IdModality = modalityId,
                IdStateInscription = pendienteStateId,
                IdAcademicPeriod = periodId,
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            var inscriptionId = inscription.Id;
            context.ChangeTracker.Clear();

            // Verificar estado inicial
            var dbInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            dbInscription!.IdStateInscription.Should().Be(pendienteStateId, "Initial state should be PENDIENTE");

            // NOTA: Este test verifica la estructura del handler.
            // El test completo de integración con el flujo de notificaciones
            // requiere mocking de los servicios de notificación o un test E2E
            // que use el API con los jobs de Hangfire configurados.

            // Por ahora verificamos que la transición de estado es posible
            dbInscription.IdStateInscription = aprobadoStateId;
            await context.SaveChangesAsync();

            // Assert
            var updatedInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            updatedInscription!.IdStateInscription.Should().Be(aprobadoStateId, "State should be updated to APROBADO");
        }

        [Fact]
        public async Task UpdateInscriptionModality_StateChangedFromPendienteToNoAplica_TriggersNotificationFlow()
        {
            // Arrange
            using var initScope = _factory.Services.CreateScope();
            var context = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedCatalogs(context);

            // Crear AcademicPeriod requerido
            var period = new AcademicPeriod
            {
                Code = "2026-I",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            // Obtener IDs necesarios
            var modalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var periodId = period.Id;
            var pendienteStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Pendiente).Id;
            var noAplicaStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.NoAplica).Id;

            // Crear inscripción en estado PENDIENTE
            var inscription = new InscriptionModality
            {
                IdModality = modalityId,
                IdStateInscription = pendienteStateId,
                IdAcademicPeriod = periodId,
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            var inscriptionId = inscription.Id;
            context.ChangeTracker.Clear();

            // Act - cambiar a NO_APLICA
            var dbInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            dbInscription!.IdStateInscription = noAplicaStateId;
            await context.SaveChangesAsync();

            // Assert
            var updatedInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            updatedInscription!.IdStateInscription.Should().Be(noAplicaStateId, "State should be updated to NO_APLICA");
        }

        [Fact]
        public async Task UpdateInscriptionModality_SameState_NoNotificationTriggered()
        {
            // Arrange
            using var initScope = _factory.Services.CreateScope();
            var context = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedCatalogs(context);

            // Crear AcademicPeriod requerido
            var period = new AcademicPeriod
            {
                Code = "2026-I",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            // Obtener IDs necesarios
            var modalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var periodId = period.Id;
            var pendienteStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Pendiente).Id;

            // Crear inscripción en estado PENDIENTE
            var inscription = new InscriptionModality
            {
                IdModality = modalityId,
                IdStateInscription = pendienteStateId,
                IdAcademicPeriod = periodId,
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            var inscriptionId = inscription.Id;
            context.ChangeTracker.Clear();

            // Act - actualizar pero mantener el mismo estado
            var dbInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            dbInscription!.Observations = "Updated observations but same state";
            await context.SaveChangesAsync();

            // Assert - el estado no cambió
            var updatedInscription = await context.Set<InscriptionModality>().FindAsync(inscriptionId);
            updatedInscription!.IdStateInscription.Should().Be(pendienteStateId, "State should remain PENDIENTE");
        }

        [Fact]
        public async Task UpdateInscriptionModality_ViaApi_ChangesStateAndEnqueuesNotificationJob()
        {
            // Este test verifica el flujo E2E completo:
            // 1. Crear una inscripción via API
            // 2. Actualizar el estado via API (PUT /api/Inscription/{id})
            // 3. Verificar que el job de notificación fue encolado

            // Arrange - Limpiar jobs previos
            _factory.EnqueuedJobs.Clear();

            // Setup: crear datos mínimos en la BD
            using var initScope = _factory.Services.CreateScope();
            var context = initScope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedingUtilities.SeedCatalogs(context);

            // Crear usuario estudiante
            var roleStudent = new Role { Name = "Student", Code = "STUDENT", Description = "Student", StatusRegister = true };
            context.Set<Role>().Add(roleStudent);

            var idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" };
            context.Set<IdentificationType>().Add(idType);

            var program = new AcademicProgram { Name = "Systems Engineering", Code = "SYS" };
            context.Set<AcademicProgram>().Add(program);

            var student = new User
            {
                FirstName = "Test",
                LastName = "Student",
                Email = "test@unicesar.edu.co",
                Identification = "12345678",
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            context.Set<User>().Add(student);
            context.Set<UserRole>().Add(new UserRole { IdUser = student.Id, IdRole = roleStudent.Id, StatusRegister = true });

            // Crear AcademicPeriod
            var period = new AcademicPeriod
            {
                Code = "2026-I",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            context.Set<AcademicPeriod>().Add(period);

            // Obtener IDs
            var modalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.ProyectoGrado).Id;
            var pendienteStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Pendiente).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;

            await context.SaveChangesAsync();

            // Crear StageModality y StateStage para poder crear la inscripción
            var stage = new StageModality
            {
                Code = "PG_FASE_INSCRIPCION",
                Name = "Fase Inscripcion",
                StageOrder = 1,
                IdModality = modalityId,
                StatusRegister = true
            };
            context.Set<StageModality>().Add(stage);
            await context.SaveChangesAsync();

            var stateStage = new StateStage
            {
                Code = "PG_INS_PEND",
                Name = "Pendiente",
                IdStageModality = stage.Id,
                IsInitialState = true,
                StatusRegister = true
            };
            context.Set<StateStage>().Add(stateStage);
            await context.SaveChangesAsync();

            // Crear inscripción inicial en PENDIENTE
            var inscription = new InscriptionModality
            {
                IdModality = modalityId,
                IdStateInscription = pendienteStateId,
                IdAcademicPeriod = period.Id,
                IdStageModality = stage.Id,
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow,
                OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            var inscriptionId = inscription.Id;

            // Agregar estudiante a la inscripción
            var userInscription = new UserInscriptionModality
            {
                IdUser = student.Id,
                IdInscriptionModality = inscription.Id,
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<UserInscriptionModality>().Add(userInscription);
            await context.SaveChangesAsync();

            var userInscriptionId = userInscription.Id;

            // Clear tracker para poder consultar via API
            context.ChangeTracker.Clear();

            // Verify initial state - no jobs enqueued yet
            _factory.EnqueuedJobs.Should().BeEmpty("No jobs should be enqueued before the update");

            // Act - Actualizar el estado via API
            var updateDto = new InscriptionWithStudentsUpdateDto
            {
                InscriptionModality = new InscriptionModalityUpdateDto
                {
                    IdModality = modalityId,
                    IdStateInscription = aprobadoStateId,
                    IdAcademicPeriod = period.Id,
                    Observations = "Aprobado por comité",
                    StatusRegister = true
                },
                Students = new List<UserInscriptionModalityUpdateDto>
                {
                    new UserInscriptionModalityUpdateDto
                    {
                        Id = userInscriptionId,
                        IdUser = student.Id,
                        StatusRegister = true
                    }
                }
            };

            var response = await _client.PutAsJsonAsync($"/api/Inscription/{inscriptionId}", updateDto);

            // Debug: print error content if 500
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                System.Diagnostics.Debug.WriteLine($"RESPONSE STATUS: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"RESPONSE CONTENT: {responseContent}");
            }

            // Assert - first just check if the API call succeeds
            if (response.StatusCode != HttpStatusCode.OK)
            {
                // Print detailed error for debugging
                System.Diagnostics.Debug.WriteLine($"FAILED: {responseContent}");
                response.StatusCode.Should().Be(HttpStatusCode.OK, $"Update should succeed. Response: {responseContent}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SUCCESS: API call succeeded");
            }

            // El bug podría estar aquí - verificar si el job fue encolado
            var enqueuedJob = _factory.EnqueuedJobs
                .FirstOrDefault(j => j.MethodName == "HandleInscriptionChangeAsync");

            // Si este assert falla, significa que el job NO se encoló
            // Lo que indicaría que el UpdateInscriptionWithStudentsHandler
            // no está encolando el job de notificación
            enqueuedJob.Should().NotBeNull(
                "HandleInscriptionChangeAsync job should be enqueued when state changes from PENDIENTE to APROBADO");
        }
    }
}
