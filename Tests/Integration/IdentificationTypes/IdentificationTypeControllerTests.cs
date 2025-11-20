using Application.Shared.DTOs.IdentificationTypes;
using Domain.Entities;

namespace Tests.Integration.IdentificationTypes
{
    public class IdentificationTypeControllerTests : GenericControllerIntegrationTests<IdentificationType, IdentificationTypeDto>
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

        protected override IdentificationType CreateValidEntity()
        {
            return new IdentificationType
            {
                Name = "Citizenship Card",
                Code = $"CC_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Colombian Citizenship Card",
                StatusRegister = true
            };
        }
    }
}