using FluentValidation;
using Application.Shared.DTOs.UserPermissions;

namespace Application.Validations.SpecificValidators.UserPermissions
{
    public class UserPermissionValidator : AbstractValidator<UserPermissionDto>
    {
        public UserPermissionValidator()
        {
            RuleFor(x => x.IdUser)
                .NotEmpty().WithMessage("El Id del Usuario es requerido.");

            RuleFor(x => x.IdPermission)
                .NotEmpty().WithMessage("El Id del Permiso es requerido.");
        }
    }
}