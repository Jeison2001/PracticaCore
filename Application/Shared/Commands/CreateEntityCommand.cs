using Application.Shared.DTOs;
using Domain.Entities;
using MediatR;

namespace Application.Shared.Commands
{
    public record CreateEntityCommand<T, TId, TDto>(TDto Dto) : IRequest<TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>;
}
