using FluentValidation;
using Application.Shared.DTOs.Permissions;

namespace Application.Validations.SpecificValidators.Permissions
{
    public class PermissionValidator : AbstractValidator<PermissionDto>
    {
        public PermissionValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(10).WithMessage("El código no puede exceder los 10 caracteres.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("La descripción es requerida.")
                .MaximumLength(255).WithMessage("La descripción no puede exceder los 255 caracteres.");
        }
    }
}