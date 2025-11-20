using Application.Shared.DTOs.StageModalities;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.StageModalities
{
    public class StageModalityControllerTests : GenericControllerIntegrationTests<StageModality, StageModalityDto>
    {
        private int _modalityId;

        public StageModalityControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/StageModality";

        protected override void SeedAdditionalData(StageModality entity)
        {
            _modalityId = SeedModalityAsync().GetAwaiter().GetResult();
            entity.IdModality = _modalityId;
        }

        private async Task<int> SeedModalityAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
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
                return modality.Id;
            }
        }

        protected override StageModality CreateValidEntity()
        {
            return new StageModality
            {
                IdModality = _modalityId,
                Code = "SM_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Stage Modality",
                StageOrder = 1,
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override StageModalityDto CreateValidDto()
        {
            return new StageModalityDto
            {
                IdModality = _modalityId,
                Code = "SM_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Stage Modality DTO",
                StageOrder = 1,
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            _modalityId = await SeedModalityAsync();
            await base.Create_ReturnsCreated();
        }
    }
}
