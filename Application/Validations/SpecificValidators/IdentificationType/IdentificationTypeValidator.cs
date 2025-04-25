using Application.Shared.DTOs.IdentificationType;
using FluentValidation;

namespace Application.Validations.SpecificValidators.IdentificationType
{
    public class IdentificationTypeValidator : AbstractValidator<IdentificationTypeDto>
    {
        public IdentificationTypeValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(20).WithMessage("El código no puede exceder los 20 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("La descripción no puede exceder los 200 caracteres.");
        }
    }
}