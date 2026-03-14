using Application.Shared.DTOs.ResearchLines;
using Domain.Entities;

namespace Tests.Integration.ResearchLines
{
    public class ResearchLineControllerTests : GenericControllerIntegrationTests<ResearchLine, ResearchLineDto>
    {
        public ResearchLineControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/ResearchLine";

        protected override ResearchLine CreateValidEntity()
        {
            return new ResearchLine
            {
                Code = "RL_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Research Line",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override ResearchLineDto CreateValidDto()
        {
            return new ResearchLineDto
            {
                Code = "RL_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Research Line DTO",
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }
    }
}