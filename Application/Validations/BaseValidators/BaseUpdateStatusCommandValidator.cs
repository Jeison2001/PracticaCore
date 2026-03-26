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
        }
    }
}
