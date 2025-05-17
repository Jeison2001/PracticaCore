using Application.Shared.DTOs.StatePreliminaryProject;
using FluentValidation;

namespace Application.Validations.SpecificValidators.StatePreliminaryProject
{
    public class StatePreliminaryProjectValidator : AbstractValidator<StatePreliminaryProjectDto>
    {
        public StatePreliminaryProjectValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("El código es requerido.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es requerido.");
        }
    }
}
