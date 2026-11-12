using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Tests.Integration.Utilities;

namespace Tests.Integration.Inscription
{
    public class CreateInscriptionWithStudentsHandlerTests : IntegrationTestBase
    {
        public CreateInscriptionWithStudentsHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateInscription_ForGradoPromedio_ShouldAssignInitialStateAndCreateExtensionRecord()
        {
            // Arrange
            // Utiliza la base de datos de pruebas configurada (MemoryDb u otra)
            using var initScope = _factory.Services.CreateScope();
            var initContext = initScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Crear dependencias para estudiante
            var roleStudent = new Role { Name = "Student", Code = "STUDENT", Description = "Student", StatusRegister = true };
            initContext.Set<Role>().Add(roleStudent);

            var idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" };
            initContext.Set<IdentificationType>().Add(idType);

            var program = new AcademicProgram { Name = "Systems Engineering", Code = "SYS" };
            initContext.Set<AcademicProgram>().Add(program);
            
            var modality = new Modality { Code = ModalityCodes.GradoPromedio, Name = "Grado Promedio", RequiresApproval = false, MaxStudents = 1, StatusRegister = true };
            initContext.Set<Modality>().Add(modality);
            
            var period = new AcademicPeriod { Code = "2026-I", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
            initContext.Set<AcademicPeriod>().Add(period);

            // Asegurarnos que los estados iniciales requeridos existan
            var stateAplica = new StateInscription { Code = StateInscriptionCodes.NoAplica, Name = "No Aplica", StatusRegister = true, IsInitialState = false };
            var statePendiente = new StateInscription { Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", StatusRegister = true, IsInitialState = true };
            initContext.Set<StateInscription>().AddRange(stateAplica, statePendiente);

            // Crear usuario estudiante
            var student = new User { FirstName = "GP", LastName = "Student", Email = "gp@test.com", Identification = "123456GP", IdIdentificationType = idType.Id, IdAcademicProgram = program.Id };
            initContext.Set<User>().Add(student);
            initContext.Set<UserRole>().Add(new UserRole { IdUser = student.Id, IdRole = roleStudent.Id, StatusRegister = true });

            await initContext.SaveChangesAsync();

            // Configurar el StageModality y StateStage correctos para el test (Fase 1 -> PENDIENTE)
            var stage = new StageModality { Code = "GPRF1", Name = "Fase 1 GP", StageOrder = 1, IdModality = modality.Id, StatusRegister = true };
            initContext.Set<StageModality>().Add(stage);
            await initContext.SaveChangesAsync();

            var stateStage = new StateStage { Code = "GPRF1-PEND", Name = "Pendiente (Fase 1)", IdStageModality = stage.Id, IsInitialState = true, StatusRegister = true };
            initContext.Set<StateStage>().Add(stateStage);
            await initContext.SaveChangesAsync();

            // Preparar el comando vía API HTTP para testear el flujo E2E
            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto 
                { 
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Prueba E2E Flujo GP"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto { Identification = student.Identification, IdIdentificationType = idType.Id }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            
            var inscriptionId = result.Data!.InscriptionModality.Id;

            // Verificaciones en Base de Datos de que todo el flujo E2E (UnitOfWork.CommitAsync -> Handlers) funcionó
            using var verificationScope = _factory.Services.CreateScope();
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var inscriptionDb = await dbContext.Set<InscriptionModality>().FindAsync(inscriptionId);
            inscriptionDb.Should().NotBeNull();
            
            // 1. Verificación del handler de creación (C#)
            inscriptionDb!.IdStateInscription.Should().Be(stateAplica.Id, "La modalidad no requiere aprobación, C# debió asignar NO_APLICA.");
            
            // 2. Verificación del Event Handler subsecuente (StartMinorModalityPhaseOnApprovalHandler)
            inscriptionDb.IdStageModality.Should().Be(stage.Id, "El manejador de eventos debió asignar la etapa inicial.");

            var academicAverageRecord = await dbContext.Set<AcademicAverage>().FindAsync(inscriptionId);
            academicAverageRecord.Should().NotBeNull("El manejador de eventos debió persistir el registro AcademicAverage.");
            academicAverageRecord!.IdStateStage.Should().Be(stateStage.Id);
        }

        [Fact]
        public async Task CreateInscription_WhenStudentHasRejectedInscription_ShouldAllowNewInscription()
        {
            // Arrange
            using var initScope = _factory.Services.CreateScope();
            var initContext = initScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var roleStudent = await initContext.Set<Role>().FirstOrDefaultAsync(r => r.Code == "STUDENT")
                ?? initContext.Set<Role>().Add(new Role { Name = "Student", Code = "STUDENT", Description = "Student", StatusRegister = true }).Entity;

            var idType = await initContext.Set<IdentificationType>().FirstOrDefaultAsync(t => t.Code == "CC")
                ?? initContext.Set<IdentificationType>().Add(new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" }).Entity;

            var program = await initContext.Set<AcademicProgram>().FirstOrDefaultAsync(p => p.Code == "SYS")
                ?? initContext.Set<AcademicProgram>().Add(new AcademicProgram { Name = "Systems Engineering", Code = "SYS" }).Entity;

            var modality = await initContext.Set<Modality>().FirstOrDefaultAsync(m => m.Code == ModalityCodes.GradoPromedio)
                ?? initContext.Set<Modality>().Add(new Modality { Code = ModalityCodes.GradoPromedio, Name = "Grado Promedio", RequiresApproval = false, MaxStudents = 1, StatusRegister = true }).Entity;

            var period = await initContext.Set<AcademicPeriod>().FirstOrDefaultAsync(p => p.Code == "2026-I")
                ?? initContext.Set<AcademicPeriod>().Add(new AcademicPeriod { Code = "2026-I", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true }).Entity;

            var stateAplica = await initContext.Set<StateInscription>().FirstOrDefaultAsync(s => s.Code == StateInscriptionCodes.NoAplica)
                ?? initContext.Set<StateInscription>().Add(new StateInscription { Code = StateInscriptionCodes.NoAplica, Name = "No Aplica", StatusRegister = true, IsInitialState = false, IsFinalStateForStage = true }).Entity;

            var statePendiente = await initContext.Set<StateInscription>().FirstOrDefaultAsync(s => s.Code == StateInscriptionCodes.Pendiente)
                ?? initContext.Set<StateInscription>().Add(new StateInscription { Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", StatusRegister = true, IsInitialState = true, IsFinalStateForStage = false }).Entity;

            var stateRechazado = await initContext.Set<StateInscription>().FirstOrDefaultAsync(s => s.Code == StateInscriptionCodes.Rechazado)
                ?? initContext.Set<StateInscription>().Add(new StateInscription { Code = StateInscriptionCodes.Rechazado, Name = "Rechazado", StatusRegister = true, IsInitialState = false, IsFinalStateForStage = true }).Entity;

            var stage = await initContext.Set<StageModality>().FirstOrDefaultAsync(s => s.Code == "GPRF1" && s.IdModality == modality.Id)
                ?? initContext.Set<StageModality>().Add(new StageModality { Code = "GPRF1", Name = "Fase 1 GP", StageOrder = 1, IdModality = modality.Id, StatusRegister = true }).Entity;

            var stateStage = await initContext.Set<StateStage>().FirstOrDefaultAsync(s => s.Code == "GPRF1-PEND" && s.IdStageModality == stage.Id)
                ?? initContext.Set<StateStage>().Add(new StateStage { Code = "GPRF1-PEND", Name = "Pendiente (Fase 1)", IdStageModality = stage.Id, IsInitialState = true, StatusRegister = true }).Entity;

            await initContext.SaveChangesAsync();

            var studentId = "REJ_" + Guid.NewGuid().ToString("N")[..8];
            var student = new User { FirstName = "Rej", LastName = "Student", Email = $"{studentId}@test.com", Identification = studentId, IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            initContext.Set<User>().Add(student);
            initContext.Set<UserRole>().Add(new UserRole { IdUser = student.Id, IdRole = roleStudent.Id, StatusRegister = true });

            var previousInscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdAcademicPeriod = period.Id,
                IdStateInscription = stateRechazado.Id,
                Observations = "Inscripción previa rechazada",
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            };
            initContext.Set<InscriptionModality>().Add(previousInscription);
            await initContext.SaveChangesAsync();

            initContext.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdUser = student.Id,
                IdInscriptionModality = previousInscription.Id,
                StatusRegister = true
            });
            await initContext.SaveChangesAsync();

            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Nueva inscripción después del rechazo"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto { Identification = student.Identification, IdIdentificationType = idType.Id }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.InscriptionModality.Id.Should().NotBe(previousInscription.Id);
        }

        [Fact]
        public async Task CreateInscription_WhenStudentHasActivePendingInscription_ShouldBlockNewInscription()
        {
            // Arrange
            using var initScope = _factory.Services.CreateScope();
            var initContext = initScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var roleStudent = await initContext.Set<Role>().FirstOrDefaultAsync(r => r.Code == "STUDENT")
                ?? initContext.Set<Role>().Add(new Role { Name = "Student", Code = "STUDENT", Description = "Student", StatusRegister = true }).Entity;

            var idType = await initContext.Set<IdentificationType>().FirstOrDefaultAsync(t => t.Code == "CC")
                ?? initContext.Set<IdentificationType>().Add(new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" }).Entity;

            var program = await initContext.Set<AcademicProgram>().FirstOrDefaultAsync(p => p.Code == "SYS")
                ?? initContext.Set<AcademicProgram>().Add(new AcademicProgram { Name = "Systems Engineering", Code = "SYS" }).Entity;

            var modality = await initContext.Set<Modality>().FirstOrDefaultAsync(m => m.Code == ModalityCodes.GradoPromedio)
                ?? initContext.Set<Modality>().Add(new Modality { Code = ModalityCodes.GradoPromedio, Name = "Grado Promedio", RequiresApproval = false, MaxStudents = 1, StatusRegister = true }).Entity;

            var period = await initContext.Set<AcademicPeriod>().FirstOrDefaultAsync(p => p.Code == "2026-I")
                ?? initContext.Set<AcademicPeriod>().Add(new AcademicPeriod { Code = "2026-I", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true }).Entity;

            var statePendiente = await initContext.Set<StateInscription>().FirstOrDefaultAsync(s => s.Code == StateInscriptionCodes.Pendiente)
                ?? initContext.Set<StateInscription>().Add(new StateInscription { Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", StatusRegister = true, IsInitialState = true, IsFinalStateForStage = false }).Entity;

            await initContext.SaveChangesAsync();

            var studentId = "PEND_" + Guid.NewGuid().ToString("N")[..8];
            var student = new User { FirstName = "Pend", LastName = "Student", Email = $"{studentId}@test.com", Identification = studentId, IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            initContext.Set<User>().Add(student);
            initContext.Set<UserRole>().Add(new UserRole { IdUser = student.Id, IdRole = roleStudent.Id, StatusRegister = true });

            var previousInscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdAcademicPeriod = period.Id,
                IdStateInscription = statePendiente.Id,
                Observations = "Inscripción pendiente activa",
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            initContext.Set<InscriptionModality>().Add(previousInscription);
            await initContext.SaveChangesAsync();

            initContext.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdUser = student.Id,
                IdInscriptionModality = previousInscription.Id,
                StatusRegister = true
            });
            await initContext.SaveChangesAsync();

            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Intento con inscripción pendiente"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto { Identification = student.Identification, IdIdentificationType = idType.Id }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("ya tiene una inscripción activa registrada"));
        }

        [Fact]
        public async Task CreateInscription_WhenStudentHasActiveApprovedInscription_ShouldBlockNewInscription()
        {
            // Arrange
            using var initScope = _factory.Services.CreateScope();
            var initContext = initScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var roleStudent = await initContext.Set<Role>().FirstOrDefaultAsync(r => r.Code == "STUDENT")
                ?? initContext.Set<Role>().Add(new Role { Name = "Student", Code = "STUDENT", Description = "Student", StatusRegister = true }).Entity;

            var idType = await initContext.Set<IdentificationType>().FirstOrDefaultAsync(t => t.Code == "CC")
                ?? initContext.Set<IdentificationType>().Add(new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" }).Entity;

            var program = await initContext.Set<AcademicProgram>().FirstOrDefaultAsync(p => p.Code == "SYS")
                ?? initContext.Set<AcademicProgram>().Add(new AcademicProgram { Name = "Systems Engineering", Code = "SYS" }).Entity;

            var modality = await initContext.Set<Modality>().FirstOrDefaultAsync(m => m.Code == ModalityCodes.GradoPromedio)
                ?? initContext.Set<Modality>().Add(new Modality { Code = ModalityCodes.GradoPromedio, Name = "Grado Promedio", RequiresApproval = false, MaxStudents = 1, StatusRegister = true }).Entity;

            var period = await initContext.Set<AcademicPeriod>().FirstOrDefaultAsync(p => p.Code == "2026-I")
                ?? initContext.Set<AcademicPeriod>().Add(new AcademicPeriod { Code = "2026-I", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true }).Entity;

            var stateAprobado = await initContext.Set<StateInscription>().FirstOrDefaultAsync(s => s.Code == StateInscriptionCodes.Aprobado)
                ?? initContext.Set<StateInscription>().Add(new StateInscription { Code = StateInscriptionCodes.Aprobado, Name = "Aprobado", StatusRegister = true, IsInitialState = false, IsFinalStateForStage = true }).Entity;

            await initContext.SaveChangesAsync();

            var studentId = "APR_" + Guid.NewGuid().ToString("N")[..8];
            var student = new User { FirstName = "Apr", LastName = "Student", Email = $"{studentId}@test.com", Identification = studentId, IdIdentificationType = idType.Id, IdAcademicProgram = program.Id, StatusRegister = true };
            initContext.Set<User>().Add(student);
            initContext.Set<UserRole>().Add(new UserRole { IdUser = student.Id, IdRole = roleStudent.Id, StatusRegister = true });

            var previousInscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdAcademicPeriod = period.Id,
                IdStateInscription = stateAprobado.Id,
                Observations = "Inscripción aprobada activa",
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            initContext.Set<InscriptionModality>().Add(previousInscription);
            await initContext.SaveChangesAsync();

            initContext.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdUser = student.Id,
                IdInscriptionModality = previousInscription.Id,
                StatusRegister = true
            });
            await initContext.SaveChangesAsync();

            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = modality.Id,
                    IdAcademicPeriod = period.Id,
                    Observations = "Intento con inscripción aprobada"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new UserInscriptionModalityCreateDto { Identification = student.Identification, IdIdentificationType = idType.Id }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Inscription", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("ya tiene una inscripción activa registrada"));
        }
    }
}
