using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.Evaluations;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Evaluations
{
    public class EvaluationChallengerTests : IntegrationTestBase
    {
        private const string AcademicPracticeBaseUrl = "/api/AcademicPractice";
        private const string EvaluationBaseUrl = "/api/Evaluation";

        public EvaluationChallengerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private (AcademicPractice practice, InscriptionModality inscription) SeedPractice(
            AppDbContext context,
            int? idUserCreated = 10,
            int? idUserUpdated = 20)
        {
            var modality = new Modality { Name = "Modality " + Guid.NewGuid(), Description = "Test" };
            context.Set<Modality>().Add(modality);
            var stateInscription = new StateInscription { Name = "State " + Guid.NewGuid() };
            context.Set<StateInscription>().Add(stateInscription);
            var period = new AcademicPeriod
            {
                Code = "P" + Guid.NewGuid().ToString("N")[..5],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6)
            };
            context.Set<AcademicPeriod>().Add(period);
            var stateStage = new StateStage { Name = "Stage " + Guid.NewGuid(), Code = "STAGE_" + Guid.NewGuid().ToString("N")[..4] };
            context.Set<StateStage>().Add(stateStage);
            context.SaveChanges();

            var inscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdStateInscription = stateInscription.Id,
                IdAcademicPeriod = period.Id
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.SaveChanges();

            var ap = new AcademicPractice
            {
                Id = inscription.Id,
                IdStateStage = stateStage.Id,
                Title = "Challenger M2 Practice " + Guid.NewGuid().ToString("N")[..4],
                InstitutionName = "Audit University",
                InstitutionContact = "audit@ufps.edu.co",
                PracticeStartDate = DateTime.UtcNow,
                PracticeEndDate = DateTime.UtcNow.AddMonths(4),
                PracticeHours = 100,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow,
                IdUserCreatedAt = idUserCreated,
                IdUserUpdatedAt = idUserUpdated
            };
            context.Set<AcademicPractice>().Add(ap);

            // Ensure EvaluationType exists
            if (!context.Set<EvaluationType>().Any(et => et.Code == EvaluationTypeCodes.AcademicPractice))
            {
                context.Set<EvaluationType>().Add(new EvaluationType
                {
                    Name = "Seguimiento Práctica Académica",
                    Code = EvaluationTypeCodes.AcademicPractice,
                    StatusRegister = true
                });
            }

            // Ensure Program and IdType exist for users
            if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
            if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

            // Ensure default evaluator user (ID 1) exists
            if (!context.Set<User>().Any(u => u.Id == 1))
            {
                context.Set<User>().Add(new User
                {
                    Id = 1,
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@unicesar.edu.co",
                    Identification = "1000000001",
                    IdIdentificationType = 1,
                    IdAcademicProgram = 1,
                    StatusRegister = true
                });
            }

            context.SaveChanges();
            return (ap, inscription);
        }

        [Fact]
        public async Task Challenge1_DeduplicationGuard_WhenObservationsMatch_StoresExactlyOneEvaluation()
        {
            // Arrange
            int practiceId;
            const string identicalObs = "Aprobado sin modificaciones para la siguiente fase.";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (ap, _) = SeedPractice(context);
                practiceId = ap.Id;
            }

            var dto = new UpdatePhaseApprovalDto
            {
                Id = practiceId,
                NewStateStageId = 1,
                Observations = identicalObs,
                EvaluatorObservations = identicalObs
            };

            // Act
            var putResponse = await _client.PutAsJsonAsync($"{AcademicPracticeBaseUrl}/UpdatePhaseApproval/{practiceId}", dto);
            putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResponse = await _client.GetAsync($"{EvaluationBaseUrl}/history/AcademicPractice/{practiceId}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Data.Should().NotBeNull();
            historyResult.Data!.Should().HaveCount(1, "Deduplication guard must ensure identical strings do not create duplicate rows");
            historyResult.Data![0].Observations.Should().Be(identicalObs);
            historyResult.Data[0].EvaluationTypeCode.Should().Be(EvaluationTypeCodes.AcademicPractice);
        }

        [Fact]
        public async Task Challenge2_EmptyOrWhitespaceObservations_CreatesNoEvaluationEntries()
        {
            // Arrange
            int practiceId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (ap, _) = SeedPractice(context);
                practiceId = ap.Id;
            }

            var dto = new UpdatePhaseApprovalDto
            {
                Id = practiceId,
                NewStateStageId = 1,
                Observations = "   ",
                EvaluatorObservations = null
            };

            // Act
            var putResponse = await _client.PutAsJsonAsync($"{AcademicPracticeBaseUrl}/UpdatePhaseApproval/{practiceId}", dto);
            putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResponse = await _client.GetAsync($"{EvaluationBaseUrl}/history/AcademicPractice/{practiceId}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Data.Should().NotBeNull();
            historyResult.Data!.Should().BeEmpty("Whitespace or null observations must not create empty rows in Evaluation table");
        }

        [Fact]
        public async Task Challenge3_UpdateInstitutionInfo_PersistsObservationHistoryCorrectly()
        {
            // Arrange
            int practiceId;
            const string institutionObs = "Observación de institución: Certificación de ARL radicada con éxito.";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (ap, _) = SeedPractice(context);
                practiceId = ap.Id;
            }

            var dto = new UpdateInstitutionInfoDto
            {
                Id = practiceId,
                Title = "Título Actualizado Institución",
                InstitutionName = "Empresa Evaluada S.A.S.",
                InstitutionContact = "contacto@empresa.com",
                PracticeStartDate = DateTime.UtcNow,
                PracticeEndDate = DateTime.UtcNow.AddMonths(4),
                PracticeHours = 120,
                Observations = institutionObs
            };

            // Act
            var putResponse = await _client.PutAsJsonAsync($"{AcademicPracticeBaseUrl}/UpdateInstitutionInfo/{practiceId}", dto);
            putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyResponse = await _client.GetAsync($"{EvaluationBaseUrl}/history/AcademicPractice/{practiceId}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Data.Should().NotBeNull();
            historyResult.Data!.Should().Contain(e => e.Observations == institutionObs);

            var record = historyResult.Data!.First(e => e.Observations == institutionObs);
            record.EvaluationTypeCode.Should().Be(EvaluationTypeCodes.AcademicPractice);
            record.EntityId.Should().Be(practiceId);
        }

        [Fact]
        public async Task Challenge4_ChronologicalOrderingAndEntityIsolation()
        {
            // Arrange
            int practiceAId;
            int practiceBId;
            const string entityType = "AcademicPractice";

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (apA, _) = SeedPractice(context);
                var (apB, _) = SeedPractice(context);
                practiceAId = apA.Id;
                practiceBId = apB.Id;

                var practiceType = context.Set<EvaluationType>().First(et => et.Code == EvaluationTypeCodes.AcademicPractice);

                // Practice A has 3 evaluations over time
                context.Set<Evaluation>().AddRange(
                    new Evaluation
                    {
                        EntityType = entityType,
                        EntityId = practiceAId,
                        IdEvaluationType = practiceType.Id,
                        IdEvaluator = 1,
                        Observations = "Oldest observation A (3 hours ago)",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow.AddHours(-3)
                    },
                    new Evaluation
                    {
                        EntityType = entityType,
                        EntityId = practiceAId,
                        IdEvaluationType = practiceType.Id,
                        IdEvaluator = 1,
                        Observations = "Middle observation A (2 hours ago)",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
                    },
                    new Evaluation
                    {
                        EntityType = entityType,
                        EntityId = practiceAId,
                        IdEvaluationType = practiceType.Id,
                        IdEvaluator = 1,
                        Observations = "Newest observation A (now)",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    // Practice B has 1 evaluation (should NOT leak into A's history)
                    new Evaluation
                    {
                        EntityType = entityType,
                        EntityId = practiceBId,
                        IdEvaluationType = practiceType.Id,
                        IdEvaluator = 1,
                        Observations = "Practice B observation",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    // Different entity type with same entityId as A (e.g. Proposal)
                    new Evaluation
                    {
                        EntityType = "Proposal",
                        EntityId = practiceAId,
                        IdEvaluationType = practiceType.Id,
                        IdEvaluator = 1,
                        Observations = "Proposal observation with same ID",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                );
                await context.SaveChangesAsync();
            }

            // Act
            var historyResponse = await _client.GetAsync($"{EvaluationBaseUrl}/history/{entityType}/{practiceAId}");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var historyResult = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyResult.Should().NotBeNull();
            historyResult!.Data.Should().NotBeNull();

            // Must have exactly 3 entries for Practice A
            historyResult.Data!.Should().HaveCount(3);
            historyResult.Data![0].Observations.Should().Be("Newest observation A (now)");
            historyResult.Data[1].Observations.Should().Be("Middle observation A (2 hours ago)");
            historyResult.Data[2].Observations.Should().Be("Oldest observation A (3 hours ago)");

            // Must not contain Practice B or Proposal observations
            historyResult.Data.Should().NotContain(e => e.Observations == "Practice B observation");
            historyResult.Data.Should().NotContain(e => e.Observations == "Proposal observation with same ID");
        }

        [Fact]
        public async Task Challenge5_EvaluationTypeFilter_ReturnsOnlyMatchingTypes()
        {
            // Arrange
            int practiceId;
            int typeAcademicPracticeId;
            int typeProposalId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (ap, _) = SeedPractice(context);
                practiceId = ap.Id;

                var typeAP = context.Set<EvaluationType>().First(et => et.Code == EvaluationTypeCodes.AcademicPractice);
                typeAcademicPracticeId = typeAP.Id;

                var typeProp = context.Set<EvaluationType>().FirstOrDefault(et => et.Code == EvaluationTypeCodes.Proposal);
                if (typeProp == null)
                {
                    typeProp = new EvaluationType
                    {
                        Name = "Propuesta",
                        Code = EvaluationTypeCodes.Proposal,
                        StatusRegister = true
                    };
                    context.Set<EvaluationType>().Add(typeProp);
                    await context.SaveChangesAsync();
                }
                typeProposalId = typeProp.Id;

                context.Set<Evaluation>().AddRange(
                    new Evaluation
                    {
                        EntityType = "AcademicPractice",
                        EntityId = practiceId,
                        IdEvaluationType = typeAcademicPracticeId,
                        IdEvaluator = 1,
                        Observations = "Academic Practice Specific Observation",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    new Evaluation
                    {
                        EntityType = "AcademicPractice",
                        EntityId = practiceId,
                        IdEvaluationType = typeProposalId,
                        IdEvaluator = 1,
                        Observations = "Other Type Observation",
                        StatusRegister = true,
                        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(5)
                    }
                );
                await context.SaveChangesAsync();
            }

            // Act - Query with filter for AcademicPractice type
            var filteredResponse = await _client.GetAsync($"{EvaluationBaseUrl}/history/AcademicPractice/{practiceId}?idEvaluationType={typeAcademicPracticeId}");
            filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert
            var filteredResult = await filteredResponse.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            filteredResult.Should().NotBeNull();
            filteredResult!.Data.Should().NotBeNull();
            filteredResult.Data!.Should().HaveCount(1);
            filteredResult.Data![0].IdEvaluationType.Should().Be(typeAcademicPracticeId);
            filteredResult.Data[0].Observations.Should().Be("Academic Practice Specific Observation");
        }
    }
}
