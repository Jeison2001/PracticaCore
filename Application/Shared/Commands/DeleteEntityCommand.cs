using Domain.Entities;
using MediatR;

namespace Application.Shared.Commands
{
    public record DeleteEntityCommand<T, TId>(TId Id) : IRequest<bool>
        where T : BaseEntity<TId>
        where TId : struct;
}
