using Api.Controllers;
using Application.Shared.DTOs.RolePermission;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class RolePermissionController : GenericController<RolePermission, int, RolePermissionDto>
    {
        public RolePermissionController(IMediator mediator) : base(mediator)
        {
        }
    }
}