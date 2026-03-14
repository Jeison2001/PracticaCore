using Application.Shared.DTOs.DocumentTypes;
using Domain.Entities;
using MediatR;

namespace Api.Controllers
{
    public class DocumentTypeController : GenericController<DocumentType, int, DocumentTypeDto>
    {
        public DocumentTypeController(IMediator mediator) : base(mediator)
        {
        }
    }
}