using Application.Shared.DTOs.Modality;

namespace Tests.Integration.Modalities
{
    public class ModalityControllerTests : GenericControllerIntegrationTests<Domain.Entities.Modality, ModalityDto>
    {
        public ModalityControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Modality";

        protected override ModalityDto CreateValidDto()
        {
            return new ModalityDto
            {
                Name = "Internship",
                Code = $"MOD_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Internship in a company",
                MaximumTermPeriods = 2,
                AllowsExtension = true,
                RequiresDirector = true,
                MaxStudents = 1,
                StatusRegister = true
            };
        }

        protected override Domain.Entities.Modality CreateValidEntity()
        {
            return new Domain.Entities.Modality
            {
                Name = "Internship",
                Code = $"MOD_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Internship in a company",
                MaximumTermPeriods = 2,
                AllowsExtension = true,
                RequiresDirector = true,
                MaxStudents = 1,
                StatusRegister = true
            };
        }
    }
}
