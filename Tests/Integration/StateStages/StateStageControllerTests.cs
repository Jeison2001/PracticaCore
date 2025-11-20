using Application.Shared.DTOs.StateStages;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.StateStages
{
    public class StateStageControllerTests : GenericControllerIntegrationTests<StateStage, StateStageDto>
    {
        private int _stageModalityId;

        public StateStageControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/StateStage";

        protected override void SeedAdditionalData(StateStage entity)
        {
            _stageModalityId = SeedStageModalityAsync().GetAwaiter().GetResult();
            entity.IdStageModality = _stageModalityId;
        }

        private async Task<int> SeedStageModalityAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                // Seed Modality first
                var modality = new Modality
                {
                    Code = "MOD_" + Guid.NewGuid().ToString().Substring(0, 8),
                    Name = "Test Modality",
                    Description = "Test Description",
                    CreatedAt = DateTime.UtcNow,
                    StatusRegister = true,
                    OperationRegister = "INSERT"
                };
                context.Set<Modality>().Add(modality);
                await context.SaveChangesAsync();

                // Seed StageModality
                var stageModality = new StageModality
                {
                    IdModality = modality.Id,
                    Code = "SM_" + Guid.NewGuid().ToString().Substring(0, 8),
                    Name = "Test Stage Modality",
                    StageOrder = 1,
                    Description = "Test Description",
                    CreatedAt = DateTime.UtcNow,
                    StatusRegister = true,
                    OperationRegister = "INSERT"
                };
                context.Set<StageModality>().Add(stageModality);
                await context.SaveChangesAsync();

                return stageModality.Id;
            }
        }

        protected override StateStage CreateValidEntity()
        {
            return new StateStage
            {
                IdStageModality = _stageModalityId,
                Code = "SS_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test State Stage",
                Description = "Test Description",
                IsInitialState = true,
                IsFinalStateForStage = false,
                IsFinalStateForModalityOverall = false,
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override StateStageDto CreateValidDto()
        {
            return new StateStageDto
            {
                IdStageModality = _stageModalityId,
                Code = "SS_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test State Stage DTO",
                Description = "Test Description DTO",
                IsInitialState = true,
                IsFinalStateForStage = false,
                IsFinalStateForModalityOverall = false,
                StatusRegister = true
            };
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            _stageModalityId = await SeedStageModalityAsync();
            await base.Create_ReturnsCreated();
        }
    }
}
