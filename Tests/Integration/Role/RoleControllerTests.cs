using Application.Shared.DTOs.Role;

namespace Tests.Integration.Roles
{
    public class RoleControllerTests : GenericControllerIntegrationTests<Domain.Entities.Role, RoleDto>
    {
        public RoleControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Role";

        protected override RoleDto CreateValidDto()
        {
            return new RoleDto
            {
                Name = "Administrator",
                Code = $"ADMIN_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "System Administrator",
                StatusRegister = true
            };
        }

        protected override Domain.Entities.Role CreateValidEntity()
        {
            return new Domain.Entities.Role
            {
                Name = "Administrator",
                Code = $"ADMIN_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "System Administrator",
                StatusRegister = true
            };
        }
    }
}
