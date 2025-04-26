using Application.Shared.DTOs.InscriptionWithStudents;
using MediatR;

namespace Application.Shared.Queries.InscriptionWithStudents
{
    public record GetInscriptionWithStudentsByUserQuery(int IdUser) : IRequest<List<InscriptionWithStudentsResponseDto>>;
}