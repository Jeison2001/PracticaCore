using Application.Shared.DTOs.Users;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class UserController : GenericController<User, int, UserDto>
    {
        public UserController(IMediator mediator) : base(mediator)
        {
        }
    }
}