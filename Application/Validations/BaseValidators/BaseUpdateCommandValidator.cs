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
            RuleFor(cmd => cmd.Id)
                .NotEqual(default(TId))
                .WithMessage("El ID es requerido y mayor a 0.");

        }
    }
}
