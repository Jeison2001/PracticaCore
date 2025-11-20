using Application.Shared.DTOs.Roles;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class RoleController : GenericController<Role, int, RoleDto>
    {
        public RoleController(IMediator mediator) : base(mediator)
        {
        }
    }
}