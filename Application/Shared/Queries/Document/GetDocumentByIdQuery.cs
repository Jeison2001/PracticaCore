using Application.Shared.DTOs.Document;
using MediatR;

namespace Application.Shared.Queries.Document
{
    public record GetDocumentByIdQuery(int Id) : IRequest<DocumentDto>;
}
