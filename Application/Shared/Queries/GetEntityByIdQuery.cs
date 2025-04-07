using Application.Shared.DTOs;
using Domain.Entities;
using MediatR;
namespace Application.Shared.Queries
{
    public record GetEntityByIdQuery<T, TId, TDto>(TId Id) : IRequest<TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>;

}
