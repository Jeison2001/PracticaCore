using Application.Shared.DTOs.InscriptionWithStudents;
using MediatR;

namespace Application.Shared.Commands.InscriptionWithStudents
{
    public record CreateInscriptionWithStudentsCommand(InscriptionWithStudentsCreateDto Dto) : IRequest<InscriptionWithStudentsDto>;
}