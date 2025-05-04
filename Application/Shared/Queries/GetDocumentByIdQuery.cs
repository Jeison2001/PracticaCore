using Application.Shared.DTOs;
using MediatR;

namespace Application.Shared.Queries
{
    public record GetDocumentByIdQuery(int Id) : IRequest<DocumentDto>;
}
