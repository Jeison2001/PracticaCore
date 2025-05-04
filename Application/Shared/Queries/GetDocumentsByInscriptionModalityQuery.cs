using Application.Shared.DTOs;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Queries
{
    public record GetDocumentsByInscriptionModalityQuery(int IdInscriptionModality) : IRequest<List<DocumentDto>>;
}
