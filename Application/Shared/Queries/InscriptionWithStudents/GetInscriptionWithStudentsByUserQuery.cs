using Application.Shared.DTOs.InscriptionWithStudents;
using MediatR;

namespace Application.Shared.Queries.InscriptionWithStudents
{
    public record GetInscriptionWithStudentsByUserQuery(int IdUser, bool? Status) : IRequest<List<InscriptionWithStudentsResponseDto>>;
}