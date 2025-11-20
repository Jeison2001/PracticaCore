using Application.Shared.DTOs.InscriptionModality;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class InscriptionModalityController : GenericController<InscriptionModality, int, InscriptionModalityDto>
    {
        public InscriptionModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}