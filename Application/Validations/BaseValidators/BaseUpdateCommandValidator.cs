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
            : this(Enumerable.Empty<IValidator<TDto>>())
        {
        }

        public BaseUpdateCommandValidator(IEnumerable<IValidator<TDto>> dtoValidators)
        {
            RuleFor(cmd => cmd.Id)
                .NotEqual(default(TId))
                .WithMessage("El ID es requerido y mayor a 0.");

            RuleFor(cmd => cmd.Dto)
                .NotNull()
                .WithMessage("Los datos de la entidad son obligatorios.");

            foreach (var dtoValidator in dtoValidators)
            {
                RuleFor(cmd => cmd.Dto)
                    .SetValidator(dtoValidator)
                    .When(cmd => cmd.Dto != null);
            }
        }
    }
}
