using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Evaluations;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Evaluations
{
    public class EvaluationControllerTests : GenericControllerIntegrationTests<Evaluation, EvaluationDto>
    {
        public EvaluationControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Evaluation";

        private async Task<(EvaluationType Type, User Evaluator, IdentificationType IdType, AcademicProgram Program)> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Tipo de Evaluación
            var type = new EvaluationType 
            { 
                Name = "Final Evaluation " + Guid.NewGuid(), 
                Code = "FINAL_" + Guid.NewGuid().ToString().Substring(0, 4), 
                StatusRegister = true 
            };
            context.Set<EvaluationType>().Add(type);

            // 2. Dependencias para User
            var idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" };
            if (!context.Set<IdentificationType>().Any(x => x.Code == "CC"))
                context.Set<IdentificationType>().Add(idType);
            else
                idType = context.Set<IdentificationType>().First(x => x.Code == "CC");

            var program = new AcademicProgram { Name = "Systems Engineering", Code = "SYS" };
            if (!context.Set<AcademicProgram>().Any(x => x.Code == "SYS"))
                context.Set<AcademicProgram>().Add(program);
            else
                program = context.Set<AcademicProgram>().First(x => x.Code == "SYS");

            await context.SaveChangesAsync();

            // 3. Usuario Evaluador
            var evaluator = new User 
            { 
                FirstName = "Evaluator", 
                LastName = "Teacher", 
                Email = "evaluator@test.com", 
                Identification = "999888777", 
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            context.Set<User>().Add(evaluator);

            await context.SaveChangesAsync();

            return (type, evaluator, idType, program);
        }

        protected override EvaluationDto CreateValidDto()
        {
            // Necesitamos ejecutar esto sincrónicamente en este contexto, o pre-seeder.
            // Como CreateValidDto es llamado por los tests de la clase base, no podemos usar await fácilmente aquí.
            // Estrategia: Usar seed "lazy" o seed en el método de test.
            // Sin embargo, la clase base llama a CreateValidDto().
            // Vamos a hacer override de los métodos de test en su lugar para asegurar que el seed ocurra.
            return new EvaluationDto(); // Placeholder
        }

        protected override Evaluation CreateValidEntity()
        {
            return new Evaluation(); // Placeholder
        }

        // Los endpoints CRUD genéricos de EvaluationController se cerraron con [NonAction]
        // para preservar la trazabilidad e inmutabilidad del historial (salvo PATCH /{id}/observations).

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            var (type, evaluator, _, _) = await SeedDependenciesAsync();

            var dto = new EvaluationDto
            {
                EntityType = "PreliminaryProject",
                EntityId = 1,
                IdEvaluationType = type.Id,
                IdEvaluator = evaluator.Id,
                Result = "Approved",
                Observations = "Excellent work",
                StatusRegister = true
            };

            var response = await _client.PostAsJsonAsync(BaseUrl, dto);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task GetAll_ReturnsOkAndList()
        {
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            var response = await _client.GetAsync($"{BaseUrl}/1");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            var (type, evaluator, _, _) = await SeedDependenciesAsync();

            var updateDto = new EvaluationDto
            {
                Id = 1,
                EntityType = "PreliminaryProject",
                EntityId = 1,
                IdEvaluationType = type.Id,
                IdEvaluator = evaluator.Id,
                Result = "Rejected",
                Observations = "Needs improvement",
                StatusRegister = true
            };

            var response = await _client.PutAsJsonAsync($"{BaseUrl}/1", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task UpdateStatus_ReturnsOk()
        {
            var updateStatusDto = new Application.Shared.DTOs.UpdateStatusRequestDto
            {
                StatusRegister = false,
                IdUserUpdateAt = 1,
                OperationRegister = "TEST_UPDATE"
            };

            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/status", updateStatusDto);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetObservationHistory_WithExistingEvaluations_ReturnsOrderedHistoryWithResolvedNames()
        {
            // Arrange
            var (type, evaluator, _, _) = await SeedDependenciesAsync();
            var entityId = new Random().Next(10000, 99999);
            var entityType = "PreliminaryProject";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var eval1 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Observations",
                    Observations = "First round observations",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
                };
                var eval2 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Approved",
                    Observations = "Second round approved",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<Evaluation>().AddRange(eval1, eval2);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);

            // Verificar orden descendente por fecha
            result.Data![0].Observations.Should().Be("Second round approved");
            result.Data[0].EvaluatorName.Should().Be("Evaluator Teacher");
            result.Data[0].EvaluationTypeCode.Should().Be(type.Code);
            result.Data[0].EvaluationTypeName.Should().Be(type.Name);

            result.Data[1].Observations.Should().Be("First round observations");
        }

        [Fact]
        public async Task GetObservationHistory_WithNonExistentEntity_ReturnsEmptyList()
        {
            // Act
            var response = await _client.GetAsync($"{BaseUrl}/history/PreliminaryProject/999999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetObservationHistory_WithMultipleDifferentEvaluators_ResolvesAllDistinctEvaluatorNames()
        {
            // Arrange
            var (type, evaluator1, idType, program) = await SeedDependenciesAsync();
            var entityId = new Random().Next(10000, 99999);
            var entityType = "PreliminaryProject";

            User evaluator2;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                evaluator2 = new User
                {
                    FirstName = "Second",
                    LastName = "Juror",
                    Email = $"juror_{Guid.NewGuid():N}@test.com",
                    Identification = new Random().Next(1000000, 9999999).ToString(),
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(evaluator2);
                await context.SaveChangesAsync();

                var eval1 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator1.Id,
                    Result = "Pending",
                    Observations = "Juror 1 feedback",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
                };
                var eval2 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator2.Id,
                    Result = "Approved",
                    Observations = "Juror 2 approval",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<Evaluation>().AddRange(eval1, eval2);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Data.Should().HaveCount(2);

            result.Data![0].EvaluatorName.Should().Be("Second Juror");
            result.Data[0].Observations.Should().Be("Juror 2 approval");

            result.Data[1].EvaluatorName.Should().Be("Evaluator Teacher");
            result.Data[1].Observations.Should().Be("Juror 1 feedback");
        }

        [Fact]
        public async Task GetObservationHistory_WithIdEvaluationTypeFilter_ReturnsOnlyMatchingType()
        {
            // Arrange
            var (type1, evaluator, _, _) = await SeedDependenciesAsync();
            var entityId = new Random().Next(10000, 99999);
            var entityType = "Proposal";

            EvaluationType type2;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                type2 = new EvaluationType
                {
                    Name = "Alternate Type " + Guid.NewGuid(),
                    Code = "ALT_" + Guid.NewGuid().ToString().Substring(0, 4),
                    StatusRegister = true
                };
                context.Set<EvaluationType>().Add(type2);
                await context.SaveChangesAsync();

                var eval1 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = type1.Id,
                    IdEvaluator = evaluator.Id,
                    Observations = "Type 1 Observation",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };
                var eval2 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = type2.Id,
                    IdEvaluator = evaluator.Id,
                    Observations = "Type 2 Observation",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<Evaluation>().AddRange(eval1, eval2);
                await context.SaveChangesAsync();
            }

            // Act: filtrar específicamente por type1.Id
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}?idEvaluationType={type1.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Data.Should().HaveCount(1);
            result.Data![0].IdEvaluationType.Should().Be(type1.Id);
            result.Data[0].Observations.Should().Be("Type 1 Observation");
        }

        [Fact]
        public async Task GetObservationHistory_WithAcademicPractice_ReturnsDescendingChronologicalHistory()
        {
            // Arrange
            var (_, evaluator, _, _) = await SeedDependenciesAsync();
            var entityId = new Random().Next(10000, 99999);
            const string entityType = "AcademicPractice";

            EvaluationType? practiceType;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                practiceType = context.Set<EvaluationType>().FirstOrDefault(et => et.Code == EvaluationTypeCodes.AcademicPractice);
                if (practiceType == null)
                {
                    practiceType = new EvaluationType
                    {
                        Name = "Seguimiento Práctica Académica",
                        Code = EvaluationTypeCodes.AcademicPractice,
                        StatusRegister = true
                    };
                    context.Set<EvaluationType>().Add(practiceType);
                    await context.SaveChangesAsync();
                }

                var eval1 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = practiceType.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Observations",
                    Observations = "Fase 0 - Plan de práctica con correcciones solicitadas.",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
                };
                var eval2 = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = practiceType.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Approved",
                    Observations = "Fase 1 - Informe de desarrollo aprobado satisfactoriamente.",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<Evaluation>().AddRange(eval1, eval2);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);

            // Verificar orden cronológico descendente (el más reciente primero)
            result.Data![0].Observations.Should().Be("Fase 1 - Informe de desarrollo aprobado satisfactoriamente.");
            result.Data[0].EvaluatorName.Should().Be($"{evaluator.FirstName} {evaluator.LastName}".Trim());
            result.Data[0].EvaluationTypeCode.Should().Be(EvaluationTypeCodes.AcademicPractice);
            result.Data[0].EvaluationTypeName.Should().Be("Seguimiento Práctica Académica");

            result.Data[1].Observations.Should().Be("Fase 0 - Plan de práctica con correcciones solicitadas.");
            result.Data[1].EvaluatorName.Should().Be($"{evaluator.FirstName} {evaluator.LastName}".Trim());
            result.Data[1].EvaluationTypeCode.Should().Be(EvaluationTypeCodes.AcademicPractice);
        }

        [Fact]
        public async Task GetObservationHistory_WithAcademicPracticeAndZeroEvaluations_ReturnsOkWithEmptyArrayAndSuccessTrue()
        {
            // Arrange
            var entityId = new Random().Next(100000, 999999);
            const string entityType = "AcademicPractice";

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetObservationHistory_WithAcademicPractice_ValidatesCompleteFrontendContractSchema()
        {
            // Arrange
            var (_, evaluator, _, _) = await SeedDependenciesAsync();
            var entityId = new Random().Next(100000, 999999);
            const string entityType = "AcademicPractice";
            const string observations = "Verificación de contrato estricto para frontend.";

            EvaluationType? practiceType;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                practiceType = context.Set<EvaluationType>().FirstOrDefault(et => et.Code == EvaluationTypeCodes.AcademicPractice);
                if (practiceType == null)
                {
                    practiceType = new EvaluationType
                    {
                        Name = "Seguimiento Práctica Académica",
                        Code = EvaluationTypeCodes.AcademicPractice,
                        StatusRegister = true
                    };
                    context.Set<EvaluationType>().Add(practiceType);
                    await context.SaveChangesAsync();
                }

                var eval = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = practiceType.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Satisfactorio",
                    Observations = observations,
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<Evaluation>().Add(eval);
                await context.SaveChangesAsync();
            }

            // Act: obtener JSON crudo para validar schema camelCase exacto del contrato
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            root.TryGetProperty("success", out var successProp).Should().BeTrue();
            successProp.GetBoolean().Should().BeTrue();

            root.TryGetProperty("data", out var dataProp).Should().BeTrue();
            dataProp.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
            dataProp.GetArrayLength().Should().Be(1);

            var firstItem = dataProp[0];
            firstItem.TryGetProperty("id", out var idProp).Should().BeTrue();
            idProp.GetInt32().Should().BeGreaterThan(0);

            firstItem.TryGetProperty("entityId", out var entityIdProp).Should().BeTrue();
            entityIdProp.GetInt32().Should().Be(entityId);

            firstItem.TryGetProperty("idEvaluator", out var idEvaluatorProp).Should().BeTrue();
            idEvaluatorProp.GetInt32().Should().Be(evaluator.Id);

            firstItem.TryGetProperty("evaluatorName", out var evaluatorNameProp).Should().BeTrue();
            evaluatorNameProp.GetString().Should().Be($"{evaluator.FirstName} {evaluator.LastName}".Trim());

            firstItem.TryGetProperty("idEvaluationType", out var idEvaluationTypeProp).Should().BeTrue();
            idEvaluationTypeProp.GetInt32().Should().Be(practiceType.Id);

            firstItem.TryGetProperty("evaluationTypeCode", out var evaluationTypeCodeProp).Should().BeTrue();
            evaluationTypeCodeProp.GetString().Should().Be(EvaluationTypeCodes.AcademicPractice);

            firstItem.TryGetProperty("evaluationTypeName", out var evaluationTypeNameProp).Should().BeTrue();
            evaluationTypeNameProp.GetString().Should().Be("Seguimiento Práctica Académica");

            firstItem.TryGetProperty("result", out var resultProp).Should().BeTrue();
            resultProp.GetString().Should().Be("Satisfactorio");

            firstItem.TryGetProperty("observations", out var observationsProp).Should().BeTrue();
            observationsProp.GetString().Should().Be(observations);

            firstItem.TryGetProperty("createdAt", out var createdAtProp).Should().BeTrue();
            createdAtProp.GetString().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetObservationHistory_WithMultipleEvaluations_EnsuresStrictDescendingChronologicalOrder()
        {
            // Arrange
            var (_, evaluator, _, _) = await SeedDependenciesAsync();
            var entityId = new Random().Next(100000, 999999);
            const string entityType = "AcademicPractice";

            EvaluationType? practiceType;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                practiceType = context.Set<EvaluationType>().FirstOrDefault(et => et.Code == EvaluationTypeCodes.AcademicPractice);
                if (practiceType == null)
                {
                    practiceType = new EvaluationType
                    {
                        Name = "Seguimiento Práctica Académica",
                        Code = EvaluationTypeCodes.AcademicPractice,
                        StatusRegister = true
                    };
                    context.Set<EvaluationType>().Add(practiceType);
                    await context.SaveChangesAsync();
                }

                var evalOldest = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = practiceType.Id,
                    IdEvaluator = evaluator.Id,
                    Observations = "Oldest: Fase 0 - Plan radicado.",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-3)
                };
                var evalMiddle = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = practiceType.Id,
                    IdEvaluator = evaluator.Id,
                    Observations = "Middle: Fase 1 - Ajuste de desarrollo.",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
                };
                var evalNewest = new Evaluation
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    IdEvaluationType = practiceType.Id,
                    IdEvaluator = evaluator.Id,
                    Observations = "Newest: Fase 2 - Aprobación informe final.",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // Insertar en orden no cronológico para verificar que la ordenación no depende del orden de inserción
                context.Set<Evaluation>().AddRange(evalMiddle, evalOldest, evalNewest);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/history/{entityType}/{entityId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(3);

            // Verificar orden estrictamente descendente
            result.Data![0].Observations.Should().Be("Newest: Fase 2 - Aprobación informe final.");
            result.Data[1].Observations.Should().Be("Middle: Fase 1 - Ajuste de desarrollo.");
            result.Data[2].Observations.Should().Be("Oldest: Fase 0 - Plan radicado.");

            result.Data[0].CreatedAt.Should().BeAfter(result.Data[1].CreatedAt);
            result.Data[1].CreatedAt.Should().BeAfter(result.Data[2].CreatedAt);
        }

        private async Task<(User CurrentUser, User OtherUser, EvaluationType EvalType)> EnsureUsersAndTypeAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var idType = await context.Set<IdentificationType>().FirstOrDefaultAsync(x => x.Code == "CC");
            if (idType == null)
            {
                idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula", StatusRegister = true };
                context.Set<IdentificationType>().Add(idType);
            }

            var program = await context.Set<AcademicProgram>().FirstOrDefaultAsync(x => x.Code == "SYS");
            if (program == null)
            {
                program = new AcademicProgram { Name = "Systems Engineering", Code = "SYS", StatusRegister = true };
                context.Set<AcademicProgram>().Add(program);
            }

            await context.SaveChangesAsync();

            var currentUser = await context.Set<User>().FirstOrDefaultAsync(u => u.Id == 1);
            if (currentUser == null)
            {
                currentUser = new User
                {
                    Id = 1,
                    FirstName = "Author",
                    LastName = "Evaluator",
                    Email = "author.eval@test.com",
                    Identification = "10000001",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(currentUser);
            }

            var otherUser = new User
            {
                FirstName = "Other",
                LastName = "Reviewer",
                Email = $"other.{Guid.NewGuid():N}@test.com",
                Identification = $"2{Guid.NewGuid().ToString("N")[..7]}",
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            context.Set<User>().Add(otherUser);

            var evalType = new EvaluationType
            {
                Name = "Review " + Guid.NewGuid(),
                Code = "REV_" + Guid.NewGuid().ToString("N")[..6],
                StatusRegister = true
            };
            context.Set<EvaluationType>().Add(evalType);

            await context.SaveChangesAsync();
            return (currentUser, otherUser, evalType);
        }

        private static async Task<(InscriptionModality Inscription, Proposal Proposal, StateStage OpenStage, StateStage FinalStage)> SeedProposalWithStagesAsync(
            AppDbContext context,
            bool startInFinalStage = false,
            bool isFinalRejection = false)
        {
            if (!await context.Set<Modality>().AnyAsync())
                Utilities.SeedingUtilities.SeedCatalogs(context);

            var modality = await context.Set<Modality>().FirstAsync();
            var stateInscription = await context.Set<StateInscription>().FirstAsync();
            var period = new AcademicPeriod
            {
                Code = "2026-" + Guid.NewGuid().ToString("N")[..4],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            context.Set<AcademicPeriod>().Add(period);

            var openStage = new StateStage
            {
                Name = "En Revision " + Guid.NewGuid().ToString("N")[..4],
                Code = "OPEN_" + Guid.NewGuid().ToString("N")[..4],
                IsFinalStateForStage = false,
                IsFinalStateForModalityOverall = false,
                IsRejectionState = false,
                StatusRegister = true
            };
            var finalStage = new StateStage
            {
                Name = "Aprobado Final " + Guid.NewGuid().ToString("N")[..4],
                Code = "FINAL_" + Guid.NewGuid().ToString("N")[..4],
                IsFinalStateForStage = true,
                IsFinalStateForModalityOverall = false,
                IsRejectionState = false,
                StatusRegister = true
            };
            context.Set<StateStage>().AddRange(openStage, finalStage);

            var line = new ResearchLine { Name = "Line " + Guid.NewGuid().ToString("N")[..4], Description = "Desc", StatusRegister = true };
            context.Set<ResearchLine>().Add(line);
            await context.SaveChangesAsync();

            var subLine = new ResearchSubLine { Name = "Sub " + Guid.NewGuid().ToString("N")[..4], Description = "Desc", IdResearchLine = line.Id, StatusRegister = true };
            context.Set<ResearchSubLine>().Add(subLine);

            var inscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdStateInscription = stateInscription.Id,
                IdAcademicPeriod = period.Id,
                IsFinalRejection = isFinalRejection,
                StatusRegister = true
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            var proposal = new Proposal
            {
                Id = inscription.Id,
                Title = "Proposal Edit Test",
                Observation = "Observación original con typo",
                IdResearchLine = line.Id,
                IdResearchSubLine = subLine.Id,
                IdStateStage = startInFinalStage ? finalStage.Id : openStage.Id,
                StatusRegister = true
            };
            context.Set<Proposal>().Add(proposal);
            await context.SaveChangesAsync();

            return (inscription, proposal, openStage, finalStage);
        }

        [Fact]
        public async Task UpdateObservation_ByAuthorOnOpenProposal_UpdatesEvaluationSyncsEntityAndReturnsEditedMetadata()
        {
            var (currentUser, _, evalType) = await EnsureUsersAndTypeAsync();
            int proposalId;
            int evaluationId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (_, proposal, _, _) = await SeedProposalWithStagesAsync(context);
                proposalId = proposal.Id;

                var eval = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = proposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = currentUser.Id,
                    Result = "Observations",
                    Observations = "Observación original con typo",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };
                context.Set<Evaluation>().Add(eval);
                await context.SaveChangesAsync();
                evaluationId = eval.Id;
            }

            var response = await _client.PatchAsJsonAsync(
                $"{BaseUrl}/{evaluationId}/observations",
                new UpdateEvaluationObservationDto { Observations = "  Observación corregida correctamente  " });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ObservationHistoryItemDto>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data!.Id.Should().Be(evaluationId);
            payload.Data.Observations.Should().Be("Observación corregida correctamente");
            payload.Data.UpdatedAt.Should().NotBeNull();
            payload.Data.IsEdited.Should().BeTrue();
            payload.Data.EvaluatorName.Should().Be($"{currentUser.FirstName} {currentUser.LastName}".Trim());

            using (var verifyScope = _factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProposal = await verifyContext.Set<Proposal>().FindAsync(proposalId);
                persistedProposal!.Observation.Should().Be("Observación corregida correctamente");
            }
        }

        [Fact]
        public async Task UpdateObservation_ByAuthorOnNonLatestEvaluation_UpdatesEvaluationWithoutOverwritingLatestEntityObservation()
        {
            var (currentUser, _, evalType) = await EnsureUsersAndTypeAsync();
            int proposalId;
            int olderEvalId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (_, proposal, _, _) = await SeedProposalWithStagesAsync(context);
                proposal.Observation = "Observación más reciente vigente";
                await context.SaveChangesAsync();
                proposalId = proposal.Id;

                var olderEval = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = proposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = currentUser.Id,
                    Observations = "Primera observación antigua",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
                };
                var newerEval = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = proposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = currentUser.Id,
                    Observations = "Observación más reciente vigente",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
                };
                context.Set<Evaluation>().AddRange(olderEval, newerEval);
                await context.SaveChangesAsync();
                olderEvalId = olderEval.Id;
            }

            var response = await _client.PatchAsJsonAsync(
                $"{BaseUrl}/{olderEvalId}/observations",
                new UpdateEvaluationObservationDto { Observations = "Primera observación aclarada" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ObservationHistoryItemDto>>();
            payload!.Data!.Observations.Should().Be("Primera observación aclarada");
            payload.Data.IsEdited.Should().BeTrue();

            using (var verifyScope = _factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProposal = await verifyContext.Set<Proposal>().FindAsync(proposalId);
                persistedProposal!.Observation.Should().Be("Observación más reciente vigente");
            }
        }

        [Fact]
        public async Task UpdateObservation_ByDifferentUser_ReturnsForbidden()
        {
            var (_, otherUser, evalType) = await EnsureUsersAndTypeAsync();
            int evalId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (_, proposal, _, _) = await SeedProposalWithStagesAsync(context);

                var eval = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = proposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = otherUser.Id,
                    Observations = "Comentario de otro evaluador",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };
                context.Set<Evaluation>().Add(eval);
                await context.SaveChangesAsync();
                evalId = eval.Id;
            }

            var response = await _client.PatchAsJsonAsync(
                $"{BaseUrl}/{evalId}/observations",
                new UpdateEvaluationObservationDto { Observations = "Intento no autorizado" });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateObservation_WhenEntityInFinalStateOrFinalRejection_ReturnsBadRequest()
        {
            var (currentUser, _, evalType) = await EnsureUsersAndTypeAsync();
            int finalStageEvalId;
            int finalRejectionEvalId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (_, closedProposal, _, _) = await SeedProposalWithStagesAsync(context, startInFinalStage: true);
                var (_, rejectedProposal, _, _) = await SeedProposalWithStagesAsync(context, startInFinalStage: false, isFinalRejection: true);

                var eval1 = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = closedProposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = currentUser.Id,
                    Observations = "Cierre final",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };
                var eval2 = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = rejectedProposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = currentUser.Id,
                    Observations = "Rechazo definitivo",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };
                context.Set<Evaluation>().AddRange(eval1, eval2);
                await context.SaveChangesAsync();
                finalStageEvalId = eval1.Id;
                finalRejectionEvalId = eval2.Id;
            }

            var respFinalStage = await _client.PatchAsJsonAsync(
                $"{BaseUrl}/{finalStageEvalId}/observations",
                new UpdateEvaluationObservationDto { Observations = "Edit bloqueado por estado final" });
            respFinalStage.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var respFinalRejection = await _client.PatchAsJsonAsync(
                $"{BaseUrl}/{finalRejectionEvalId}/observations",
                new UpdateEvaluationObservationDto { Observations = "Edit bloqueado por rechazo definitivo" });
            respFinalRejection.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateObservation_WithWhitespace_ReturnsBadRequest()
        {
            var (currentUser, _, evalType) = await EnsureUsersAndTypeAsync();
            int evalId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (_, proposal, _, _) = await SeedProposalWithStagesAsync(context);

                var eval = new Evaluation
                {
                    EntityType = nameof(Proposal),
                    EntityId = proposal.Id,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = currentUser.Id,
                    Observations = "Válida",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };
                context.Set<Evaluation>().Add(eval);
                await context.SaveChangesAsync();
                evalId = eval.Id;
            }

            var response = await _client.PatchAsJsonAsync(
                $"{BaseUrl}/{evalId}/observations",
                new UpdateEvaluationObservationDto { Observations = "    " });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}

