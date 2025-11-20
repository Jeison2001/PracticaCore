using Application.Shared.DTOs.ResearchGroups;
using Domain.Entities;

namespace Tests.Integration.ResearchGroups
{
    public class ResearchGroupControllerTests : GenericControllerIntegrationTests<ResearchGroup, ResearchGroupDto>
    {
        public ResearchGroupControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/ResearchGroup";

        protected override ResearchGroup CreateValidEntity()
        {
            return new ResearchGroup
            {
                Code = "RG_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Research Group",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override ResearchGroupDto CreateValidDto()
        {
            return new ResearchGroupDto
            {
                Code = "RG_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Research Group DTO",
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }
    }
}