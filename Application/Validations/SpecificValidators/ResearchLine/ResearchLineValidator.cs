using Application.Shared.DTOs.ResearchLine;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ResearchLine
{
    public class ResearchLineValidator : AbstractValidator<ResearchLineDto>
    {
        public ResearchLineValidator()
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