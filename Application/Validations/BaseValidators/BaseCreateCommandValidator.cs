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
        {
        }
    }
}