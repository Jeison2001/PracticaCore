using Application.Shared.DTOs.Faculty;
using Domain.Entities;
using Tests.Integration;

namespace Tests.Integration.Faculties
{
    public class FacultyControllerTests : GenericControllerIntegrationTests<Domain.Entities.Faculty, FacultyDto>
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

        protected override Domain.Entities.Faculty CreateValidEntity()
        {
            return new Domain.Entities.Faculty
            {
                Name = "Engineering",
                Code = $"ENG_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StatusRegister = true
            };
        }
    }
}
