using Application.Shared.DTOs.Faculties;
using Domain.Entities;

namespace Tests.Integration.Faculties
{
    public class FacultyControllerTests : GenericControllerIntegrationTests<Faculty, FacultyDto>
    {
        public FacultyControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Faculty";

        protected override FacultyDto CreateValidDto()
        {
            return new FacultyDto
            {
                Name = "Engineering New",
                Code = $"ENG_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StatusRegister = true
            };
        }

        protected override Faculty CreateValidEntity()
        {
            return new Faculty
            {
                Name = "Engineering",
                Code = $"ENG_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StatusRegister = true
            };
        }
    }
}