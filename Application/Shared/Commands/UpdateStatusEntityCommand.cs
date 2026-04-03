using Domain.Common.Users;
using Domain.Entities;
using MediatR;

namespace Application.Shared.Commands
{
    public record UpdateStatusEntityCommand<T, TId>(
        TId Id,
        bool StatusRegister,
        CurrentUserInfo CurrentUser,
        string OperationRegister
    ) : IRequest<bool>
        where T : BaseEntity<TId>
        where TId : struct;
}
