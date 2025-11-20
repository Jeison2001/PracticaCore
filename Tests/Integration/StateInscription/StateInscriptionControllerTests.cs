using Application.Shared.DTOs.StateInscription;

namespace Tests.Integration.StateInscriptions
{
    public class StateInscriptionControllerTests : GenericControllerIntegrationTests<Domain.Entities.StateInscription, StateInscriptionDto>
    {
        public StateInscriptionControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/StateInscription";

        protected override Domain.Entities.StateInscription CreateValidEntity()
        {
            return new Domain.Entities.StateInscription
            {
                Code = "SI_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test State Inscription",
                Description = "Test Description",
                IsInitialState = true,
                IsFinalStateForStage = false,
                CreatedAt = DateTime.UtcNow,
                StatusRegister = true,
                OperationRegister = "INSERT"
            };
        }

        protected override StateInscriptionDto CreateValidDto()
        {
            return new StateInscriptionDto
            {
                Code = "SI_" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = "Test State Inscription DTO",
                Description = "Test Description DTO",
                IsInitialState = true,
                IsFinalStateForStage = false,
                StatusRegister = true
            };
        }
    }
}
