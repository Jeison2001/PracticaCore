using Application.Shared.DTOs;
using Application.Shared.Queries;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    public class BaseQueryByIdValidator<TQuery, TEntity, TId, TDto>
    : AbstractValidator<TQuery>
    where TQuery : GetEntityByIdQuery<TEntity, TId, TDto>
    where TEntity : BaseEntity<TId>
    where TDto : BaseDto<TId>
    where TId : struct
    {
        public BaseQueryByIdValidator()
        {
            RuleFor(query => query.Id)
                .NotEqual(default(TId)) // Mejor que NotEmpty para tipos struct
                .WithMessage("El ID es requerido y mayor a 0.");
        }
    }
}
