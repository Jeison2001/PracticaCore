using Application.Shared.DTOs.InscriptionModalities;
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