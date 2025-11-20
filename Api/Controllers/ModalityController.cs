using Application.Shared.DTOs.Modality;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class ModalityController : GenericController<Modality, int, ModalityDto>
    {
        public ModalityController(IMediator mediator) : base(mediator)
        {
        }
    }
}