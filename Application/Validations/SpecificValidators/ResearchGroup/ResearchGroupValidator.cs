using Application.Shared.DTOs.ResearchGroup;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ResearchGroup
{
    public class ResearchGroupValidator : AbstractValidator<ResearchGroupDto>
    {
        public ResearchGroupValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(100).WithMessage("El código no puede exceder los 100 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");
        }
    }
}