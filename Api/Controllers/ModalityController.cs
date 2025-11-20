using Application.Shared.DTOs.Modalities;
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