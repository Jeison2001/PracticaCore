using Application.Shared.Commands;
using Application.Shared.DTOs;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    public class BaseCreateEntitiesCommandValidator<TEntity, TDto, TId>
        : AbstractValidator<CreateEntitiesCommand<TEntity, TId, TDto>>
        where TEntity : BaseEntity<TId>
        where TDto : BaseDto<TId>
        where TId : struct
    {
        public BaseCreateEntitiesCommandValidator()
            : this(Enumerable.Empty<IValidator<TDto>>())
        {
        }

        public BaseCreateEntitiesCommandValidator(IEnumerable<IValidator<TDto>> dtoValidators)
        {
            RuleFor(cmd => cmd.Dtos)
                .NotEmpty()
                .WithMessage("Debe proporcionar al menos un elemento.");

            foreach (var dtoValidator in dtoValidators)
            {
                RuleForEach(cmd => cmd.Dtos)
                    .SetValidator(dtoValidator);
            }
        }
    }
}
