using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.Commands.AcademicPractices;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.Evaluations;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.AcademicPractices
{
    /// <summary>
    /// Empirical stress test suite for evaluation history persistence across phase transitions
    /// in PracticaCore (R2.1 - R2.4).
    /// </summary>
    public class AcademicPracticeEvaluationStressTests : IntegrationTestBase
    {
        private const string BaseUrl = "/api/AcademicPractice";

        public AcademicPracticeEvaluationStressTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private AcademicPractice CreateAndSeedAcademicPractice(int? userCreatedAt = null, int? userUpdatedAt = null)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            EnsurePrerequisites(context);

            var modality = new Modality { Name = "Modality " + Guid.NewGuid(), Description = "Test" };
            context.Set<Modality>().Add(modality);
            var stateInscription = new StateInscription { Name = "State " + Guid.NewGuid() };
            context.Set<StateInscription>().Add(stateInscription);
            var academicPeriod = new AcademicPeriod
            {
                Code = "AP-" + Guid.NewGuid().ToString().Substring(0, 4),
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(6)
            };
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

            var stateStage = new StateStage { Name = "Stage " + Guid.NewGuid() };
            context.Set<StateStage>().Add(stateStage);
            context.SaveChanges();

            var entity = new AcademicPractice
            {
                Id = inscription.Id,
                IdStateStage = stateStage.Id,
                Title = "Stress Test Practice",
                InstitutionName = "Test Hospital",
                InstitutionContact = "555-1234",
                PracticeStartDate = DateTime.Now,
                PracticeEndDate = DateTime.Now.AddMonths(4),
                PracticeHours = 120,
                IdUserCreatedAt = userCreatedAt,
                IdUserUpdatedAt = userUpdatedAt
            };

            context.Set<AcademicPractice>().Add(entity);
            context.SaveChanges();

            return entity;
        }

        private static void EnsurePrerequisites(AppDbContext context)
        {
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

            EnsureUser(context, 1, "Admin", "User");
            context.SaveChanges();
        }

        private static void EnsureUser(AppDbContext context, int userId, string firstName = "User", string lastName = "Test")
        {
            var user = context.Set<User>().Find(userId);
            if (user == null)
            {
                context.Set<User>().Add(new User
                {
                    Id = userId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"user{userId}@unicesar.edu.co",
                    Identification = $"1090{userId}",
                    IdIdentificationType = 1,
                    IdAcademicProgram = 1,
                    StatusRegister = true
                });
            }
        }

        #region Criterion 1: Empty and Whitespace Observations Handling

        [Theory]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData(null, "")]
        [InlineData("   ", null)]
        public async Task StressTest_EmptyOrWhitespaceObservations_DoesNotInsertEvaluationRecords(string? obs, string? evalObs)
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = obs,
                EvaluatorObservations = evalObs
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert: Query observation history endpoint
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Success.Should().BeTrue();
            historyResult.Data.Should().BeEmpty("no evaluation record should be inserted for empty or whitespace observations");
        }

        [Fact]
        public async Task StressTest_OneEmptyAndOneValidObservation_InsertsOnlyValidRecord()
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();
            const string validEval = "Observación de evaluador válida";

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = "", // Empty general observation
                EvaluatorObservations = validEval
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult!.Data.Should().HaveCount(1);
            historyResult.Data![0].Observations.Should().Be(validEval);
        }

        [Fact]
        public async Task StressTest_ValidGeneralAndEmptyEvaluatorObservation_InsertsOnlyGeneralRecord()
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();
            const string validGen = "Observación general válida";

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = validGen,
                EvaluatorObservations = "   " // Whitespace evaluator observation
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult!.Data.Should().HaveCount(1);
            historyResult.Data![0].Observations.Should().Be(validGen);
        }

        #endregion

        #region Criterion 2: Identical vs Distinct Observations in DTO

        [Fact]
        public async Task StressTest_IdenticalObservations_DeduplicatesToSingleRecord()
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();
            const string identicalText = "El plan de práctica cumple con los requisitos mínimos.";

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = identicalText,
                EvaluatorObservations = identicalText
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult!.Data.Should().HaveCount(1, "identical observations must be deduplicated to a single Evaluation record");
            historyResult.Data![0].Observations.Should().Be(identicalText);
        }

        [Fact]
        public async Task StressTest_DistinctObservations_PersistsBothRecordsSeparately()
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();
            const string evalText = "Evaluador: Aprobar con 4.5";
            const string genText = "Comité: Se autoriza la continuación de fase.";

            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = genText,
                EvaluatorObservations = evalText
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", updateDto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult!.Data.Should().HaveCount(2, "distinct observations must both be preserved");
            historyResult.Data!.Select(x => x.Observations).Should().Contain([evalText, genText]);
        }

        #endregion

        #region Criterion 3: Multiple Consecutive Review Iterations & Chronological Ordering

        [Fact]
        public async Task StressTest_MultipleConsecutiveReviews_AccumulatesHistoryInDescendingChronologicalOrder()
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();

            const string review1 = "Iteración 1: Correcciones al plan inicial requeridas.";
            const string review2 = "Iteración 2: Plan ajustado y aprobado para desarrollo.";
            const string review3 = "Iteración 3: Informe final presentado y evaluado exitosamente.";

            // Act: Iteration 1
            var dto1 = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = review1,
                EvaluatorObservations = review1
            };
            var resp1 = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", dto1);
            resp1.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(50); // Ensure distinct CreatedAt timestamp

            // Act: Iteration 2
            var dto2 = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = review2,
                EvaluatorObservations = review2
            };
            var resp2 = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", dto2);
            resp2.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(50); // Ensure distinct CreatedAt timestamp

            // Act: Iteration 3
            var dto3 = new UpdatePhaseApprovalDto
            {
                Id = entity.Id,
                NewStateStageId = entity.IdStateStage,
                Observations = review3,
                EvaluatorObservations = review3
            };
            var resp3 = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{entity.Id}", dto3);
            resp3.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert: Verify accumulated history and ordering
            var historyResponse = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Data.Should().HaveCount(3, "all 3 consecutive reviews must be accumulated");

            var items = historyResult.Data!;
            // Descending chronological order: newest first
            items[0].Observations.Should().Be(review3);
            items[1].Observations.Should().Be(review2);
            items[2].Observations.Should().Be(review1);

            items[0].CreatedAt.Should().BeOnOrAfter(items[1].CreatedAt);
            items[1].CreatedAt.Should().BeOnOrAfter(items[2].CreatedAt);

            // Verify metadata across all records
            foreach (var item in items)
            {
                item.EntityId.Should().Be(entity.Id);
                item.EvaluationTypeCode.Should().Be(EvaluationTypeCodes.AcademicPractice);
                item.EvaluationTypeName.Should().Be("Seguimiento Práctica Académica");
                item.IdEvaluator.Should().Be(1);
                item.EvaluatorName.Should().NotBeNullOrWhiteSpace();
            }
        }

        #endregion

        #region Criterion 4: Missing or Null CurrentUser.UserId Fallback Hierarchy

        [Fact]
        public async Task StressTest_NullUserId_CascadesToEntityIdUserUpdatedAt()
        {
            // Arrange: CurrentUser.UserId is null, entity has IdUserUpdatedAt = 42
            const int updatedByUserId = 42;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                EnsureUser(context, updatedByUserId, "Docente", "Actualizador");
                context.SaveChanges();
            }

            var entity = CreateAndSeedAcademicPractice(userCreatedAt: 99, userUpdatedAt: updatedByUserId);

            var command = new UpdateAcademicPracticePhaseCommand(
                new UpdatePhaseApprovalDto
                {
                    Id = entity.Id,
                    NewStateStageId = entity.IdStateStage,
                    Observations = "Prueba fallback IdUserUpdatedAt",
                    EvaluatorObservations = "Prueba fallback IdUserUpdatedAt"
                },
                new CurrentUserInfo { UserId = null }); // Missing CurrentUser.UserId

            // Act: Execute via MediatR handler directly
            using (var scope = _factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(command);
                result.Should().BeTrue();
            }

            // Assert: Query DB for evaluation record
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var eval = context.Set<Evaluation>()
                    .FirstOrDefault(e => e.EntityType == nameof(AcademicPractice) && e.EntityId == entity.Id);

                eval.Should().NotBeNull();
                eval!.IdEvaluator.Should().Be(updatedByUserId, "evaluatorId should fall back to entity.IdUserUpdatedAt when CurrentUser.UserId is null");
                eval.Observations.Should().Be("Prueba fallback IdUserUpdatedAt");
            }
        }

        [Fact]
        public async Task StressTest_NullUserId_AndNullUpdatedAt_CascadesToEntityIdUserCreatedAt()
        {
            // Arrange: CurrentUser.UserId is null, IdUserUpdatedAt is null, entity has IdUserCreatedAt = 77
            const int createdByUserId = 77;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                EnsureUser(context, createdByUserId, "Docente", "Creador");
                context.SaveChanges();
            }

            var entity = CreateAndSeedAcademicPractice(userCreatedAt: createdByUserId, userUpdatedAt: null);

            var command = new UpdateAcademicPracticePhaseCommand(
                new UpdatePhaseApprovalDto
                {
                    Id = entity.Id,
                    NewStateStageId = entity.IdStateStage,
                    Observations = "Prueba fallback IdUserCreatedAt",
                    EvaluatorObservations = "Prueba fallback IdUserCreatedAt"
                },
                new CurrentUserInfo { UserId = null });

            // Act
            using (var scope = _factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(command);
                result.Should().BeTrue();
            }

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var eval = context.Set<Evaluation>()
                    .FirstOrDefault(e => e.EntityType == nameof(AcademicPractice) && e.EntityId == entity.Id);

                eval.Should().NotBeNull();
                eval!.IdEvaluator.Should().Be(createdByUserId, "evaluatorId should fall back to entity.IdUserCreatedAt when CurrentUser.UserId and IdUserUpdatedAt are null");
            }
        }

        [Fact]
        public async Task StressTest_NullUserId_AndAllEntityUserIdsNull_CascadesToDefaultSystemUser1()
        {
            // Arrange: CurrentUser.UserId is null, IdUserUpdatedAt is null, IdUserCreatedAt is null
            var entity = CreateAndSeedAcademicPractice(userCreatedAt: null, userUpdatedAt: null);

            var command = new UpdateAcademicPracticePhaseCommand(
                new UpdatePhaseApprovalDto
                {
                    Id = entity.Id,
                    NewStateStageId = entity.IdStateStage,
                    Observations = "Prueba fallback usuario sistema 1",
                    EvaluatorObservations = "Prueba fallback usuario sistema 1"
                },
                new CurrentUserInfo { UserId = null });

            // Act
            using (var scope = _factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(command);
                result.Should().BeTrue();
            }

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var eval = context.Set<Evaluation>()
                    .FirstOrDefault(e => e.EntityType == nameof(AcademicPractice) && e.EntityId == entity.Id);

                eval.Should().NotBeNull();
                eval!.IdEvaluator.Should().Be(1, "evaluatorId should fall back to 1 (system admin) when all other user IDs are null");
            }
        }

        [Fact]
        public async Task StressTest_InstitutionUpdateCommand_NullUserId_CascadesToSystemUser1()
        {
            // Arrange: Test UpdateAcademicPracticeInstitutionCommand with null CurrentUser.UserId
            var entity = CreateAndSeedAcademicPractice(userCreatedAt: null, userUpdatedAt: null);

            var command = new UpdateAcademicPracticeInstitutionCommand(
                new UpdateInstitutionInfoDto
                {
                    Id = entity.Id,
                    Title = "Updated Institution Title",
                    InstitutionName = "Hospital San Jose",
                    InstitutionContact = "contacto@hospital.org",
                    PracticeStartDate = DateTimeOffset.UtcNow,
                    PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(3),
                    PracticeHours = 160,
                    Observations = "Observaciones de actualización institucional"
                },
                new CurrentUserInfo { UserId = null });

            // Act
            using (var scope = _factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(command);
                result.Should().BeTrue();
            }

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var eval = context.Set<Evaluation>()
                    .FirstOrDefault(e => e.EntityType == nameof(AcademicPractice) && e.EntityId == entity.Id);

                eval.Should().NotBeNull();
                eval!.IdEvaluator.Should().Be(1, "Institution command evaluatorId should also fall back safely to 1");
                eval.Observations.Should().Be("Observaciones de actualización institucional");
            }
        }

        #endregion

        #region Adversarial Challenge: Entity Isolation & Non-Existent Entity

        [Fact]
        public async Task StressTest_NonExistentPracticeId_ReturnsNotFoundAndCreatesNoEvaluations()
        {
            // Arrange
            const int nonExistentId = 999999;
            var updateDto = new UpdatePhaseApprovalDto
            {
                Id = nonExistentId,
                NewStateStageId = 1,
                Observations = "Debería fallar",
                EvaluatorObservations = "Debería fallar"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{nonExistentId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var eval = context.Set<Evaluation>()
                .FirstOrDefault(e => e.EntityType == nameof(AcademicPractice) && e.EntityId == nonExistentId);
            eval.Should().BeNull("no evaluations may be recorded for a non-existent practice");
        }

        [Fact]
        public async Task StressTest_EntityIsolation_DoesNotCrossPolluteEvaluationHistories()
        {
            // Arrange: Create two separate academic practices
            var practiceA = CreateAndSeedAcademicPractice();
            var practiceB = CreateAndSeedAcademicPractice();

            const string obsA = "Observación exclusiva Práctica A";
            const string obsB = "Observación exclusiva Práctica B";

            // Act: Update Practice A
            await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{practiceA.Id}", new UpdatePhaseApprovalDto
            {
                Id = practiceA.Id,
                NewStateStageId = practiceA.IdStateStage,
                Observations = obsA,
                EvaluatorObservations = obsA
            });

            // Act: Update Practice B
            await _client.PutAsJsonAsync($"{BaseUrl}/UpdatePhaseApproval/{practiceB.Id}", new UpdatePhaseApprovalDto
            {
                Id = practiceB.Id,
                NewStateStageId = practiceB.IdStateStage,
                Observations = obsB,
                EvaluatorObservations = obsB
            });

            // Assert: Query Practice A history
            var respA = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{practiceA.Id}");
            var histA = await respA.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            histA!.Data.Should().HaveCount(1);
            histA.Data![0].Observations.Should().Be(obsA);
            histA.Data.Should().NotContain(x => x.Observations == obsB);

            // Assert: Query Practice B history
            var respB = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{practiceB.Id}");
            var histB = await respB.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            histB!.Data.Should().HaveCount(1);
            histB.Data![0].Observations.Should().Be(obsB);
            histB.Data.Should().NotContain(x => x.Observations == obsA);
        }

        [Fact]
        public async Task StressTest_HistoryQuery_TypeFiltering_ReturnsOnlyMatchingType()
        {
            // Arrange
            var entity = CreateAndSeedAcademicPractice();

            int type1Id;
            int type2Id;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var t1 = context.Set<EvaluationType>().First(et => et.Code == EvaluationTypeCodes.AcademicPractice);
                type1Id = t1.Id;

                var t2 = new EvaluationType
                {
                    Name = "Otro Tipo Evaluación",
                    Code = "OTRO_TIPO",
                    StatusRegister = true
                };
                context.Set<EvaluationType>().Add(t2);
                context.SaveChanges();
                type2Id = t2.Id;

                // Add evaluations of both types for entity
                context.Set<Evaluation>().AddRange(
                    new Evaluation
                    {
                        EntityType = nameof(AcademicPractice),
                        EntityId = entity.Id,
                        IdEvaluator = 1,
                        IdEvaluationType = type1Id,
                        Observations = "Tipo 1 Obs",
                        StatusRegister = true
                    },
                    new Evaluation
                    {
                        EntityType = nameof(AcademicPractice),
                        EntityId = entity.Id,
                        IdEvaluator = 1,
                        IdEvaluationType = type2Id,
                        Observations = "Tipo 2 Obs",
                        StatusRegister = true
                    }
                );
                context.SaveChanges();
            }

            // Act: Query filtered by type 1
            var resp1 = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}?idEvaluationType={type1Id}");
            var hist1 = await resp1.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();

            // Assert
            hist1!.Data.Should().HaveCount(1);
            hist1.Data![0].Observations.Should().Be("Tipo 1 Obs");

            // Act: Query filtered by type 2
            var resp2 = await _client.GetAsync($"/api/Evaluation/history/AcademicPractice/{entity.Id}?idEvaluationType={type2Id}");
            var hist2 = await resp2.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();

            // Assert
            hist2!.Data.Should().HaveCount(1);
            hist2.Data![0].Observations.Should().Be("Tipo 2 Obs");
        }

        #endregion
    }
}
