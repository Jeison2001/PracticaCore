using Application.Shared.DTOs.ResearchSubLine;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ResearchSubLine
{
    public class ResearchSubLineValidator : AbstractValidator<ResearchSubLineDto>
    {
        public ResearchSubLineValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(100).WithMessage("El código no puede exceder los 100 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");

            RuleFor(x => x.IdResearchLine)
                .NotEmpty().WithMessage("La línea de investigación es obligatoria.");
        }
    }
}