using Application.Shared.DTOs.IdentificationType;
using Domain.Entities;
using Tests.Integration;

namespace Tests.Integration.IdentificationTypes
{
    public class IdentificationTypeControllerTests : GenericControllerIntegrationTests<Domain.Entities.IdentificationType, IdentificationTypeDto>
    {
        public IdentificationTypeControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/IdentificationType";

        protected override IdentificationTypeDto CreateValidDto()
        {
            return new IdentificationTypeDto
            {
                Name = "Citizenship Card",
                Code = $"CC_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Colombian Citizenship Card",
                StatusRegister = true
            };
        }

        protected override Domain.Entities.IdentificationType CreateValidEntity()
        {
            return new Domain.Entities.IdentificationType
            {
                Name = "Citizenship Card",
                Code = $"CC_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Colombian Citizenship Card",
                StatusRegister = true
            };
        }
    }
}
