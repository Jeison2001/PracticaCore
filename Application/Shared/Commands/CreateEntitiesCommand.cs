using Application.Shared.DTOs;
using Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Commands
{
    public record CreateEntitiesCommand<T, TId, TDto>(List<TDto> Dtos) : IRequest<List<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>;
}
