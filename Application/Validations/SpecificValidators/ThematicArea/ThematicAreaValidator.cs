using Application.Shared.DTOs.ThematicArea;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ThematicArea
{
    public class ThematicAreaValidator : AbstractValidator<ThematicAreaDto>
    {
        public ThematicAreaValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(150).WithMessage("El código no puede exceder los 150 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");

            RuleFor(x => x.IdResearchSubLine)
                .NotEmpty().WithMessage("La sublínea de investigación es obligatoria.");
        }
    }
}