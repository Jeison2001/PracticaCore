using Application.Shared.DTOs.PreliminaryProjects;
using FluentValidation;

namespace Application.Validations.SpecificValidators.PreliminaryProjects
{
    public class PreliminaryProjectValidator : AbstractValidator<PreliminaryProjectDto>
    {
        public PreliminaryProjectValidator()
        {
            RuleFor(x => x.IdStateStage).GreaterThan(0).WithMessage("Debe tener un estado válido.");
        }
    }
}
