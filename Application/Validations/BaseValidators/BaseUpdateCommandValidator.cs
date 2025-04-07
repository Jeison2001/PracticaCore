using Application.Shared.Commands;
using Application.Shared.DTOs;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    public class BaseUpdateCommandValidator<TEntity, TDto, TId>
    : AbstractValidator<UpdateEntityCommand<TEntity, TId, TDto>>
    where TEntity : BaseEntity<TId>
    where TDto : BaseDto<TId>
    where TId : struct
    {
        public BaseUpdateCommandValidator()
        {
            // Validar ID del comando
            RuleFor(cmd => cmd.Id)
                .NotEqual(default(TId))
                .WithMessage("El ID es requerido y mayor a 0.");

            // Validar propiedades del DTO
            RuleFor(cmd => cmd.Dto.OperationRegister)
                .NotEmpty().WithMessage("La operación es requerida.");

            RuleFor(cmd => cmd.Dto.IdUserUpdatedAt)
                .NotEqual(default(int?))
                .WithMessage("El usuario de actualización es requerido.");

            RuleFor(cmd => cmd.Dto.StatusRegister)
                .NotNull().WithMessage("El estado es requerido.");
        }
    }
}
