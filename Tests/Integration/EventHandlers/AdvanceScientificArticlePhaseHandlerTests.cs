using Application.Features.MinorModalities.EventHandlers;
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
    public class AdvanceScientificArticlePhaseHandlerTests : IntegrationTestBase
    {
        public AdvanceScientificArticlePhaseHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!context.Set<Modality>().Any())
                SeedingUtilities.SeedCatalogs(context);
        }

        private AdvanceScientificArticlePhaseHandler CreateHandler() =>
            new(_scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AdvanceScientificArticlePhaseHandler>>());

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 1: Articulo reaches Fase1_Aprobado (IsFinalStateForStage=true, NOT Overall)
        //             → advances to Fase 2 initial state
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_ArticuloFase1Aprobado_AdvancesToFase2()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = new User
            {
                Id = 1, Email = "articulo@test.edu.co", FirstName = "Art", LastName = "Test",
                Identification = "001", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var artModalityId = context.Set<Modality>().First(m => m.Code == ModalityCodes.PublicacionArticulo).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;
            var fase1StageId = context.Set<StageModality>().First(s => s.Code == "PA_FASE_1").Id;

            var inscription = new InscriptionModality
            {
                Id = 1, IdModality = artModalityId, IdStageModality = fase1StageId,
                IdStateInscription = aprobadoStateId, IdAcademicPeriod = 1,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);

            // The article starts in Fase 1 initial state
            var fase1InicialStateId = context.Set<StateStage>().First(s => s.Code == "PC_FASEINICIAL").Id;
            var articulo = new ScientificArticle
            {
                Id = inscription.Id,
                IdStateStage = fase1InicialStateId,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<ScientificArticle>().Add(articulo);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // The "trigger state" is PC_FASE1_APROBADO: IsFinalStateForStage=true, IsFinalStateForModalityOverall=false
            var fase1AprobadoStateId = context.Set<StateStage>().First(s => s.Code == "PC_FASE1_APROBADO").Id;

            var domainEvent = new ScientificArticleStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: artModalityId,
                NewStateStageId: fase1AprobadoStateId,
                OldStateStageId: fase1InicialStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Article should now be in the initial state of Fase 2
            var updatedArticle = await actContext.Set<ScientificArticle>().FindAsync(articulo.Id);
            updatedArticle.Should().NotBeNull();
            var fase2InicialStateId = actContext.Set<StateStage>().First(s => s.Code == "PC_FASE2_INICIAL").Id;
            updatedArticle!.IdStateStage.Should().Be(fase2InicialStateId,
                "al alcanzar el estado final de Fase 1. el artículo debe avanzar al estado inicial de Fase 2");

            // 2. Inscription should be in Fase 2
            var updatedInscription = await actContext.Set<InscriptionModality>().FindAsync(inscription.Id);
            var fase2StageId = actContext.Set<StageModality>().First(s => s.Code == "PA_FASE_2").Id;
            updatedInscription!.IdStageModality.Should().Be(fase2StageId,
                "la InscriptionModality debe avanzar a la Fase 2 del Artículo");
        }

        // ──────────────────────────────────────────────────────────────────────────────
        // Scenario 2: Guard: IsFinalStateForModalityOverall=true → should NOT advance
        // ──────────────────────────────────────────────────────────────────────────────
        [Fact]
        public async Task Handle_FinalStateForModalityOverall_DoesNotAdvance()
        {
            // Arrange
            var context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = new User
            {
                Id = 2, Email = "articulo2@test.edu.co", FirstName = "Art2", LastName = "Test2",
                Identification = "002", StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<User>().Add(user);

            var artModalityId   = context.Set<Modality>().First(m => m.Code == ModalityCodes.PublicacionArticulo).Id;
            var aprobadoStateId = context.Set<StateInscription>().First(s => s.Code == StateInscriptionCodes.Aprobado).Id;
            var fase2StageId    = context.Set<StageModality>().First(s => s.Code == "PA_FASE_2").Id;

            var inscription = new InscriptionModality
            {
                Id = 2, IdModality = artModalityId, IdStageModality = fase2StageId,
                IdStateInscription = aprobadoStateId, IdAcademicPeriod = 1,
                CreatedAt = DateTime.UtcNow, StatusRegister = true, OperationRegister = "Test"
            };
            context.Set<InscriptionModality>().Add(inscription);

            // A "final overall" state (e.g. Fase 2 final aprobado — use PC_FASE2_INICIAL as the current)
            var fase2InicialStateId = context.Set<StateStage>().First(s => s.Code == "PC_FASE2_INICIAL").Id;

            // Create a synthetic final-overall state in Fase 2
            var finalOverallState = new StateStage
            {
                Id = 50, Code = "PC_APROBADO_FINAL",
                IdStageModality = fase2StageId,
                IsInitialState = false,
                IsFinalStateForStage = true,
                IsFinalStateForModalityOverall = true,
                StatusRegister = true,
                OperationRegister = "Test",
                Name = "Artículo Aprobado Final"
            };
            context.Set<StateStage>().Add(finalOverallState);

            var articulo = new ScientificArticle
            {
                Id = inscription.Id,
                IdStateStage = fase2InicialStateId,
                IdUserCreatedAt = user.Id,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            context.Set<ScientificArticle>().Add(articulo);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var domainEvent = new ScientificArticleStateChangedEvent(
                InscriptionModalityId: inscription.Id,
                ModalityId: artModalityId,
                NewStateStageId: finalOverallState.Id, // FinalForStage=true AND FinalOverall=true → guard blocks
                OldStateStageId: fase2InicialStateId,
                TriggeredByUserId: user.Id
            );

            // Act
            await CreateHandler().Handle(domainEvent, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert - article state and inscription stage must remain unchanged
            var actContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedArticle = await actContext.Set<ScientificArticle>().FindAsync(articulo.Id);
            updatedArticle!.IdStateStage.Should().Be(fase2InicialStateId,
                "cuando IsFinalStateForModalityOverall=true el handler no debe avanzar");
        }
    }
}
