using Api.Controllers;
using Application.Shared.DTOs.Permission;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class PermissionController : GenericController<Permission, long, PermissionDto>
    {
        public PermissionController(IMediator mediator) : base(mediator)
        {
        }
    }
}