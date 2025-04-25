using Api.Controllers;
using Application.Shared.DTOs.User;
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