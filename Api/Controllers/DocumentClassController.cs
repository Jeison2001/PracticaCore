using Api.Controllers;
using Application.Shared.DTOs.DocumentClass;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class DocumentClassController : GenericController<DocumentClass, int, DocumentClassDto>
    {
        public DocumentClassController(IMediator mediator) : base(mediator)
        {
        }
    }
}
