using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.AcademicPractices;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Common;
using Infrastructure.Data;
using Domain.Constants;
using Application.Shared.DTOs.Evaluations;

namespace Tests.Integration.AcademicPractices
{
    public class AcademicPracticeControllerTests : GenericControllerIntegrationTests<AcademicPractice, AcademicPracticeDto>
    {
        protected override string BaseUrl => "/api/AcademicPractice";

        public AcademicPracticeControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override AcademicPracticeDto CreateValidDto()
        {
            return new AcademicPracticeDto
            {
                Title = "Test Practice",
                InstitutionName = "Test Inst",
                InstitutionContact = "Contact",
                PracticeStartDate = DateTime.Now,
                PracticeEndDate = DateTime.Now.AddMonths(3),
                PracticeHours = 100,
                Observations = "Obs"
            };
        }

        protected override AcademicPractice CreateValidEntity()
        {
            return new AcademicPractice
            {
                Title = "Test Practice",
                InstitutionName = "Test Inst",
                InstitutionContact = "Contact",
                PracticeStartDate = DateTime.Now,
                PracticeEndDate = DateTime.Now.AddMonths(3),
                PracticeHours = 100,
                Observations = "Obs"
            };
        }

        protected override void SeedAdditionalData(AcademicPractice entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Seed de cadena InscriptionModality
                var modality = new Modality { Name = "Test Modality AP " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State AP " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-AP" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                // 2. Seed de StateStage
                var stateStage = new StateStage { Name = "Stage AP " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                context.SaveChanges();

                // Establecer ID de entidad y FKs
                entity.Id = inscription.Id; // 1:1 with Inscription
                entity.IdStateStage = stateStage.Id;
            }
        }

        // Override de GetAll porque el controlador usa un endpoint personalizado /GetAll (WithDetails)
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        // Override de GetById porque el controlador usa /WithDetails/{id}
        public override async Task GetById_ReturnsOkAndEntity()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeWithDetailsResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AcademicPractice!.Id.Should().Be(entity.Id);
        }

        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            var dto = CreateValidDto();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Seed de InscriptionModality
                var modality = new Modality { Name = "Test Modality AP Create " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State AP Create " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-APC" + Guid.NewGuid().ToString().Substring(0,4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                // Seed de StateStage
                var stateStage = new StateStage { Name = "Stage AP Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                context.SaveChanges();

                // Actualizar DTO
                dto.Id = inscription.Id;
                dto.IdStateStage = stateStage.Id;
            }

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(dto.Id);
        }

        [Fact]
        public async Task UpdateInstitutionInfo_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new UpdateInstitutionInfoDto
            {
                Id = entity.Id,
                Title = "Updated Title",
                InstitutionName = "Updated Inst",
                InstitutionContact = "Updated Contact",
                PracticeStartDate = DateTime.Now.AddDays(1),
                PracticeEndDate = DateTime.Now.AddDays(5),
                PracticeHours = 80
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdateInstitutionInfo/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();

            // Verificar en BD
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dbEntity = await context.Set<AcademicPractice>().FindAsync(entity.Id);
                dbEntity.Should().NotBeNull();
                dbEntity!.Title.Should().Be("Updated Title");
                dbEntity.PracticeHours.Should().Be(80);
            }
        }

        [Fact]
        public async Task UpdatePhaseApproval_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage, // Usar estado existente o uno nuevo
                Observations = "Approved",
                EvaluatorObservations = "Approved"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task UpdatePhaseApproval_PersistsEvaluationObservationHistory()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);

            const string expectedObservations = "Se aprueba el plan inicial con ajustes menores.";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);

                // Asegurar EvaluationType para Prácticas Académicas
                if (!context.Set<EvaluationType>().Any(et => et.Code == EvaluationTypeCodes.AcademicPractice))
                {
                    context.Set<EvaluationType>().Add(new EvaluationType
                    {
                        Name = "Seguimiento Práctica Académica",
                        Code = EvaluationTypeCodes.AcademicPractice,
                        StatusRegister = true
                    });
                }

                // Asegurar dependencias de User 1 (usuario autenticado en TestAuthHandler)
                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var evaluatorUser = context.Set<User>().Find(1);
                if (evaluatorUser == null)
                {
                    context.Set<User>().Add(new User
                    {
                        Id = 1,
                        FirstName = "Carlos",
                        LastName = "Docente",
                        Email = "test@unicesar.edu.co",
                        Identification = "1090123456",
                        IdIdentificationType = 1,
                        IdAcademicProgram = 1,
                        StatusRegister = true
                    });
                }
                else
                {
                    evaluatorUser.FirstName = "Carlos";
                    evaluatorUser.LastName = "Docente";
                }

                await context.SaveChangesAsync();
            }

            // Enviamos EvaluatorObservations y Observations idénticos para validar persistencia y deduplicación
            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = expectedObservations,
                EvaluatorObservations = expectedObservations
            };

            // Act 1: Actualizar aprobación de fase
            var updateResponse = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);

            // Assert 1: PUT exitoso
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act 2: Consultar historial cronológico de observaciones para AcademicPractice
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");

            // Assert 2: Historial retornado con datos del evaluador, tipo y observaciones
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Success.Should().BeTrue();
            historyResult.Data.Should().NotBeNull();
            // Deduplicación: aunque ambos campos en DTO tenían texto, solo debe persistirse un registro
            historyResult.Data!.Should().HaveCount(1);

            var recorded = historyResult.Data!.First();
            recorded.EntityId.Should().Be(entity.Id);
            recorded.IdEvaluator.Should().Be(1);
            recorded.EvaluatorName.Should().Be("Carlos Docente");
            recorded.EvaluationTypeCode.Should().Be(EvaluationTypeCodes.AcademicPractice);
            recorded.EvaluationTypeName.Should().Be("Seguimiento Práctica Académica");
            recorded.Observations.Should().Be(expectedObservations);
        }

        [Fact]
        public async Task UpdatePhaseApproval_WithDistinctObservations_PersistsBothEvaluationRecords()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);

            const string evalObs = "Concepto de jurado: Aprobado con recomendaciones.";
            const string generalObs = "Observación de comité: Validación de acta de inicio.";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);

                if (!context.Set<EvaluationType>().Any(et => et.Code == EvaluationTypeCodes.AcademicPractice))
                {
                    context.Set<EvaluationType>().Add(new EvaluationType
                    {
                        Name = "Seguimiento Práctica Académica",
                        Code = EvaluationTypeCodes.AcademicPractice,
                        StatusRegister = true
                    });
                }

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var evaluatorUser = context.Set<User>().Find(1);
                if (evaluatorUser == null)
                {
                    context.Set<User>().Add(new User
                    {
                        Id = 1,
                        FirstName = "Carlos",
                        LastName = "Docente",
                        Email = "test@unicesar.edu.co",
                        Identification = "1090123456",
                        IdIdentificationType = 1,
                        IdAcademicProgram = 1,
                        StatusRegister = true
                    });
                }

                await context.SaveChangesAsync();
            }

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = generalObs,
                EvaluatorObservations = evalObs
            };

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Data.Should().NotBeNull();
            // Ambos registros distintos deben coexistir
            historyResult.Data!.Should().HaveCount(2);
            historyResult.Data!.Should().Contain(e => e.Observations == evalObs);
            historyResult.Data!.Should().Contain(e => e.Observations == generalObs);
        }

        [Fact]
        public async Task GetByUserId_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int userId = 1;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                
                // Seed de User y UserInscriptionModality
                var user = new User 
                { 
                    Id = userId,
                    FirstName = "Test", 
                    LastName = "User", 
                    Email = "test@test.com", 
                    Identification = "123",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                if (!context.Set<User>().Any(u => u.Id == userId))
                    context.Set<User>().Add(user);

                var userInscription = new UserInscriptionModality
                {
                    IdUser = userId,
                    IdInscriptionModality = entity.Id
                };
                context.Set<UserInscriptionModality>().Add(userInscription);

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByUser/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data!.First().AcademicPractice!.Id.Should().Be(entity.Id);
        }

        [Fact]
        public async Task GetByTeacherId_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int teacherId = 2;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);
                
                // Seed de Teacher (User) y TeachingAssignment
                var teacher = new User 
                { 
                    Id = teacherId,
                    FirstName = "Teacher", 
                    LastName = "User", 
                    Email = "teacher@test.com", 
                    Identification = "456",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<User>().Any(u => u.Id == teacherId))
                    context.Set<User>().Add(teacher);

                // Seed de TypeTeachingAssignment
                var typeAssignment = new TypeTeachingAssignment { Name = "Director", Description = "Director" };
                context.Set<TypeTeachingAssignment>().Add(typeAssignment);
                await context.SaveChangesAsync();

                var assignment = new TeachingAssignment
                {
                    IdTeacher = teacherId,
                    IdInscriptionModality = entity.Id,
                    IdTypeTeachingAssignment = typeAssignment.Id
                };
                context.Set<TeachingAssignment>().Add(assignment);

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByTeacher/{teacherId}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data!.Items.First().AcademicPractice!.Id.Should().Be(entity.Id);
            result.Data!.Items.First().Teachers.Should().NotBeEmpty();
            result.Data!.Items.First().Teachers.First().IdTeacher.Should().Be(teacherId);
        }

        [Fact]
        public async Task GetAllWithDetails_WithActiveTeachers_ReturnsCompleteAssignedTeachersData()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int activeTeacherId = 201;
            int revokedTeacherId = 202;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                // Active Teacher
                var activeTeacher = new User
                {
                    Id = activeTeacherId,
                    FirstName = "Carlos",
                    LastName = "Docente",
                    Email = "carlos.docente@ufps.edu.co",
                    Identification = "T201",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<User>().Any(u => u.Id == activeTeacherId))
                    context.Set<User>().Add(activeTeacher);

                // Revoked Teacher
                var revokedTeacher = new User
                {
                    Id = revokedTeacherId,
                    FirstName = "Pedro",
                    LastName = "Revocado",
                    Email = "pedro.revocado@ufps.edu.co",
                    Identification = "T202",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<User>().Any(u => u.Id == revokedTeacherId))
                    context.Set<User>().Add(revokedTeacher);

                // Cargo Type
                var cargo = new TypeTeachingAssignment
                {
                    Code = "JURADO_EVALUADOR",
                    Name = "Jurado Evaluador",
                    Description = "Docente evaluador de práctica"
                };
                context.Set<TypeTeachingAssignment>().Add(cargo);
                await context.SaveChangesAsync();

                // Active Teaching Assignment
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = activeTeacherId,
                    IdInscriptionModality = entity.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true,
                    RevocationDate = null
                });

                // Revoked Teaching Assignment
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = revokedTeacherId,
                    IdInscriptionModality = entity.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true,
                    RevocationDate = DateTimeOffset.UtcNow.AddDays(-1)
                });

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var item = result.Data!.Items.FirstOrDefault(i => i.AcademicPractice?.Id == entity.Id);
            item.Should().NotBeNull();
            item!.Teachers.Should().NotBeEmpty();

            // Verify active teacher is present with all required fields
            var teacher = item.Teachers.FirstOrDefault(t => t.IdTeacher == activeTeacherId);
            teacher.Should().NotBeNull();
            teacher!.FullName.Should().Be("Carlos Docente");
            teacher.Email.Should().Be("carlos.docente@ufps.edu.co");
            teacher.CargoCode.Should().Be("JURADO_EVALUADOR");
            teacher.CargoName.Should().Be("Jurado Evaluador");
            teacher.RevocationDate.Should().BeNull();

            // Verify backward compatibility accessors
            teacher.IdUser.Should().Be(activeTeacherId);
            teacher.IdTypeTeachingAssignment.Should().Be(teacher.CargoId);
            teacher.AssignmentType.Should().Be("Jurado Evaluador");
            teacher.StatusRegister.Should().BeTrue();

            // Verify revoked teacher is excluded
            item.Teachers.Should().NotContain(t => t.IdTeacher == revokedTeacherId);
        }

        [Fact]
        public async Task GetAllWithDetails_FilterByTeacherName_ReturnsOnlyMatchingPractices()
        {
            // Arrange
            var entity1 = CreateValidEntity();
            entity1.Title = "Practice Alpha";
            SeedAdditionalData(entity1);

            var entity2 = CreateValidEntity();
            entity2.Title = "Practice Beta";
            SeedAdditionalData(entity2);

            int teacherId1 = 301;
            int teacherId2 = 302;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().AddRange(entity1, entity2);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var teacher1 = new User
                {
                    Id = teacherId1,
                    FirstName = "Mariana",
                    LastName = "Ramirez",
                    Email = "mariana.ramirez@ufps.edu.co",
                    Identification = "T301",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                var teacher2 = new User
                {
                    Id = teacherId2,
                    FirstName = "Roberto",
                    LastName = "Gomez",
                    Email = "roberto.gomez@ufps.edu.co",
                    Identification = "T302",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<User>().Any(u => u.Id == teacherId1))
                    context.Set<User>().Add(teacher1);
                if (!context.Set<User>().Any(u => u.Id == teacherId2))
                    context.Set<User>().Add(teacher2);

                var cargo = new TypeTeachingAssignment { Code = "DOCENTE_REVISOR", Name = "Docente Revisor", Description = "Revisor" };
                context.Set<TypeTeachingAssignment>().Add(cargo);
                await context.SaveChangesAsync();

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacherId1,
                    IdInscriptionModality = entity1.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacherId2,
                    IdInscriptionModality = entity2.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // Act - Filter by Mariana
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=Mariana");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Must include entity1 (assigned to Mariana) and exclude entity2 (assigned to Roberto)
            result.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == entity1.Id);
            result.Data!.Items.Should().NotContain(i => i.AcademicPractice!.Id == entity2.Id);
        }

        [Fact]
        public async Task GetById_WithDetails_ReturnsPopulatedTeachers()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            int teacherId = 401;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<AcademicPractice>().Add(entity);

                if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                    context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
                if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                    context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

                var teacher = new User
                {
                    Id = teacherId,
                    FirstName = "Laura",
                    LastName = "Suarez",
                    Email = "laura.suarez@ufps.edu.co",
                    Identification = "T401",
                    IdAcademicProgram = 1,
                    IdIdentificationType = 1
                };
                if (!context.Set<User>().Any(u => u.Id == teacherId))
                    context.Set<User>().Add(teacher);

                var cargo = new TypeTeachingAssignment { Code = "ASESOR", Name = "Asesor", Description = "Asesor" };
                context.Set<TypeTeachingAssignment>().Add(cargo);
                await context.SaveChangesAsync();

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacherId,
                    IdInscriptionModality = entity.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeWithDetailsResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AcademicPractice!.Id.Should().Be(entity.Id);
            result.Data!.Teachers.Should().NotBeEmpty();

            var loadedTeacher = result.Data!.Teachers.First();
            loadedTeacher.IdTeacher.Should().Be(teacherId);
            loadedTeacher.FullName.Should().Be("Laura Suarez");
            loadedTeacher.Email.Should().Be("laura.suarez@ufps.edu.co");
            loadedTeacher.CargoCode.Should().Be("ASESOR");
            loadedTeacher.CargoName.Should().Be("Asesor");
        }
    }
}
