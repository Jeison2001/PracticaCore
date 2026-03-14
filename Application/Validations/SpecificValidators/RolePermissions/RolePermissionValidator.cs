using FluentValidation;
using Application.Shared.DTOs.RolePermissions;

namespace Application.Validations.SpecificValidators.RolePermissions
{
    public class RolePermissionValidator : AbstractValidator<RolePermissionDto>
    {
        public RolePermissionValidator()
        {
            RuleFor(x => x.IdRole).GreaterThan(0).WithMessage("IdRole debe ser mayor a 0.");
            RuleFor(x => x.IdPermission).GreaterThan(0).WithMessage("IdPermission debe ser mayor a 0.");
        }
    }
}