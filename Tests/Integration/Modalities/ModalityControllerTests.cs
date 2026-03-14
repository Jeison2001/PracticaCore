using Application.Shared.DTOs.Modalities;
using Domain.Entities;

namespace Tests.Integration.Modalities
{
    public class ModalityControllerTests : GenericControllerIntegrationTests<Modality, ModalityDto>
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
                RequiresSimpleDocumentation = true,
                StatusRegister = true
            };
        }

        protected override Modality CreateValidEntity()
        {
            return new Modality
            {
                Name = "Internship",
                Code = $"MOD_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Internship in a company",
                MaximumTermPeriods = 2,
                AllowsExtension = true,
                RequiresDirector = true,
                MaxStudents = 1,
                RequiresSimpleDocumentation = true,
                StatusRegister = true
            };
        }
    }
}