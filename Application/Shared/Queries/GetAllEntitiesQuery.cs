using Application.Shared.DTOs;
using Domain.Entities;
using MediatR;

namespace Application.Shared.Queries
{
    public record GetAllEntitiesQuery<T, TId, TDto> : IRequest<IEnumerable<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>;
}
