using Application.Shared.DTOs.RegisterModalityState;
using FluentValidation;

namespace Application.Validations.specificsValidators.RegisterModalityState
{
    public class RegisterModalityStateValidator : AbstractValidator<RegisterModalityStateDto>
    {
        public RegisterModalityStateValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
        }
    }
}