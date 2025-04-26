using Application.Shared.DTOs.InscriptionWithStudents;
using MediatR;

namespace Application.Shared.Queries.InscriptionWithStudents
{
    public record GetInscriptionWithStudentsQuery(int Id) : IRequest<InscriptionWithStudentsResponseDto>;
}