using Application.Shared.DTOs.ProjectFinal;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ProjectFinal
{
    public class ProjectFinalValidator : AbstractValidator<ProjectFinalDto>
    {
        public ProjectFinalValidator()
        {
            RuleFor(x => x.IdStateProjectFinal).GreaterThan(0).WithMessage("Debe tener un estado final válido.");
        }
    }
}
