using Application.Shared.Commands;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    public class BaseDeleteCommandValidator<TEntity, TId>
    : AbstractValidator<DeleteEntityCommand<TEntity, TId>>
    where TEntity : BaseEntity<TId>
    where TId : struct
    {
        public BaseDeleteCommandValidator()
        {
            RuleFor(cmd => cmd.Id)
                .NotEqual(default(TId))
                .WithMessage("El ID es requerido y mayor a 0.");
        }
    }
}
