using Application.Shared.DTOs.Permissions;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class PermissionController : GenericController<Permission, int, PermissionDto>
    {
        public PermissionController(IMediator mediator) : base(mediator)
        {
        }
    }
}