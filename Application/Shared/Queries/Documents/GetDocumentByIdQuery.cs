using Application.Shared.DTOs.Documents;
using MediatR;

namespace Application.Shared.Queries.Documents
{
    public record GetDocumentByIdQuery(int Id) : IRequest<DocumentDto>;
}
