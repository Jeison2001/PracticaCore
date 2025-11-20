using Application.Shared.DTOs.AcademicPeriod;

namespace Tests.Integration.AcademicPeriods
{
    public class AcademicPeriodControllerTests : GenericControllerIntegrationTests<Domain.Entities.AcademicPeriod, AcademicPeriodDto>
    {
        public AcademicPeriodControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/AcademicPeriod";

        protected override AcademicPeriodDto CreateValidDto()
        {
            return new AcademicPeriodDto
            {
                Code = $"PER_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
        }

        protected override Domain.Entities.AcademicPeriod CreateValidEntity()
        {
            return new Domain.Entities.AcademicPeriod
            {
                Code = $"PER_{Guid.NewGuid().ToString().Substring(0, 8)}",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
        }
    }
}
