using Application.Shared.DTOs.UserInscriptionModalities;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class UserInscriptionModalityController : GenericController<UserInscriptionModality, int, UserInscriptionModalityDto>
    {
        public UserInscriptionModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}