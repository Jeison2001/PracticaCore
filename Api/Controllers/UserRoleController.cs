using Api.Controllers;
using Application.Shared.DTOs.UserRole;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class UserRoleController : GenericController<UserRole, int, UserRoleDto>
    {
        public UserRoleController(IMediator mediator) : base(mediator)
        {
        }
    }
}