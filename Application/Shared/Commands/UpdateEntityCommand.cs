using Application.Shared.DTOs;
using Domain.Common.Users;
using Domain.Entities;
using MediatR;

namespace Application.Shared.Commands
{
    public record UpdateEntityCommand<T, TId, TDto>(TId Id, TDto Dto, CurrentUserInfo CurrentUser) : IRequest<TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>;
}
