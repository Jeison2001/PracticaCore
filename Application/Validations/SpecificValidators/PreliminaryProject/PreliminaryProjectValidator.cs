using Application.Shared.DTOs.PreliminaryProject;
using FluentValidation;

namespace Application.Validations.SpecificValidators.PreliminaryProject
{
    public class PreliminaryProjectValidator : AbstractValidator<PreliminaryProjectDto>
    {
        public PreliminaryProjectValidator()
        {
            RuleFor(x => x.IdStateStage).GreaterThan(0).WithMessage("Debe tener un estado válido.");
        }
    }
}
