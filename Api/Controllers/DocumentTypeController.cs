using Api.Controllers;
using Application.Shared.DTOs;
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