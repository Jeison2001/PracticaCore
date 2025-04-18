using FluentValidation;
using Application.Shared.DTOs.UserPermission;

namespace Application.Validations.SpecificValidators.UserPermission
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