using Application.Shared.DTOs.ResearchSubLines;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Domain.Entities;

namespace Tests.Integration.ResearchSubLines
{
    public class ResearchSubLineControllerTests : GenericControllerIntegrationTests<ResearchSubLine, ResearchSubLineDto>
    {
        private int _researchLineId;

        public ResearchSubLineControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/ResearchSubLine";

        protected override void SeedAdditionalData(ResearchSubLine entity)
        {
            _researchLineId = SeedResearchLineAsync().GetAwaiter().GetResult();
            entity.IdResearchLine = _researchLineId;
        }

        private async Task<int> SeedResearchLineAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var researchLine = new ResearchLine
                {
                    Code = "RL_" + Guid.NewGuid().ToString().Substring(0, 5),
                    Name = "Research Line Test",
                    Description = "Description",
                    OperationRegister = "INSERT",
                    StatusRegister = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Set<ResearchLine>().Add(researchLine);
                await context.SaveChangesAsync();

                return researchLine.Id;
            }
        }

        protected override ResearchSubLine CreateValidEntity()
        {
            return new ResearchSubLine
            {
                IdResearchLine = _researchLineId,
                Code = "RSL_" + Guid.NewGuid().ToString().Substring(0, 5),
                Name = "Research SubLine Test",
                Description = "Test Description",
                OperationRegister = "INSERT",
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        protected override ResearchSubLineDto CreateValidDto()
        {
            return new ResearchSubLineDto
            {
                IdResearchLine = _researchLineId,
                Code = "RSL_" + Guid.NewGuid().ToString().Substring(0, 5),
                Name = "Research SubLine DTO",
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            _researchLineId = await SeedResearchLineAsync();
            await base.Create_ReturnsCreated();
        }
    }
}