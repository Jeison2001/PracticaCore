using Api.Controllers;
using Application.Shared.DTOs.Role;
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