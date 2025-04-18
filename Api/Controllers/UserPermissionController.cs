using Api.Controllers;
using Application.Shared.DTOs.UserPermission;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class UserPermissionController : GenericController<UserPermission, long, UserPermissionDto>
    {
        public UserPermissionController(IMediator mediator) : base(mediator)
        {
        }
    }
}