using Application.Shared.DTOs.EvaluationTypes;
using Domain.Entities;

namespace Tests.Integration.EvaluationTypes
{
    public class EvaluationTypeControllerTests : GenericControllerIntegrationTests<EvaluationType, EvaluationTypeDto>
    {
        public EvaluationTypeControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/EvaluationType";

        protected override EvaluationType CreateValidEntity()
        {
            return new EvaluationType
            {
                Code = "ET_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Evaluation Type",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override EvaluationTypeDto CreateValidDto()
        {
            return new EvaluationTypeDto
            {
                Code = "ET_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test Evaluation Type DTO",
                Description = "Test Description DTO",
                StatusRegister = true
            };
        }
    }
}