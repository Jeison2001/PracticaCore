using Application.Shared.Commands;
using Application.Shared.DTOs;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    public class BaseCreateCommandValidator<TEntity, TDto, TId>
    : AbstractValidator<CreateEntityCommand<TEntity, TId, TDto>>
    where TEntity : BaseEntity<TId>
    where TDto : BaseDto<TId>
    where TId : struct
    {
        public BaseCreateCommandValidator()
            : this(Enumerable.Empty<IValidator<TDto>>())
        {
        }

        public BaseCreateCommandValidator(IEnumerable<IValidator<TDto>> dtoValidators)
        {
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