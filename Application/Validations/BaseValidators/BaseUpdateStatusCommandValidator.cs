using Application.Shared.Commands;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    public class BaseUpdateStatusCommandValidator<TEntity, TId>
    : AbstractValidator<UpdateStatusEntityCommand<TEntity, TId>>
    where TEntity : BaseEntity<TId>
    where TId : struct
    {
        public BaseUpdateStatusCommandValidator()
        {
            RuleFor(cmd => cmd.Id)
                .NotEqual(default(TId))
                .WithMessage("El ID es requerido y mayor a 0.");

            RuleFor(cmd => cmd.StatusRegister)
                .NotNull()
                .WithMessage("El estado es requerido.");

            RuleFor(cmd => cmd.IdUserUpdateAt)
                .GreaterThan(0)
                .WithMessage("El ID del usuario que actualiza debe ser mayor a 0.");

            RuleFor(cmd => cmd.OperationRegister)
                .NotEmpty()
                .WithMessage("La operación es requerida.")
                .MaximumLength(100)
                .WithMessage("La operación no puede exceder los 100 caracteres.");
        }
    }
}
