using Application.Shared.DTOs;
using Domain.Common.Users;
using Domain.Entities;
using MediatR;

namespace Application.Shared.Commands
{
    public record CreateEntitiesCommand<T, TId, TDto>(List<TDto> Dtos, CurrentUserInfo CurrentUser) : IRequest<List<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>;
}
