using Application.Shared.DTOs.ProjectFinals;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ProjectFinals
{
    public class ProjectFinalValidator : AbstractValidator<ProjectFinalDto>
    {
        public ProjectFinalValidator()
        {
            RuleFor(x => x.IdStateStage).GreaterThan(0).WithMessage("Debe tener un estado final válido.");
        }
    }
}
