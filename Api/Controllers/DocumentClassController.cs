using Application.Shared.DTOs.DocumentsClasses;
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
