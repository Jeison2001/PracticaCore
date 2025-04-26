using Application.Shared.DTOs.InscriptionWithStudents;
using MediatR;

namespace Application.Shared.Commands.InscriptionWithStudents
{
    public record UpdateInscriptionWithStudentsCommand(int Id, InscriptionWithStudentsUpdateDto Dto) : IRequest<InscriptionWithStudentsDto>;
}