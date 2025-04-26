using Api.Controllers;
using Application.Shared.DTOs.UserInscriptionModality;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public class UserInscriptionModalityController : GenericController<UserInscriptionModality, int, UserInscriptionModalityDto>
    {
        public UserInscriptionModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}