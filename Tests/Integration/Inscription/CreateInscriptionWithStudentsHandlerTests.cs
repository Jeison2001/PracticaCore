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
    }
}
