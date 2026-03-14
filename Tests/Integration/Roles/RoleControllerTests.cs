using Application.Shared.DTOs.Roles;
using Domain.Entities;

namespace Tests.Integration.Roles
{
    public class RoleControllerTests : GenericControllerIntegrationTests<Role, RoleDto>
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

        protected override Role CreateValidEntity()
        {
            return new Role
            {
                Name = "Administrator",
                Code = $"ADMIN_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "System Administrator",
                StatusRegister = true
            };
        }
    }
}