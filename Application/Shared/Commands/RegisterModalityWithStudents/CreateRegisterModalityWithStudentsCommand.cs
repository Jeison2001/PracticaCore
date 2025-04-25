using Application.Shared.DTOs.RegisterModalityWithStudents;
using MediatR;

namespace Application.Shared.Commands.RegisterModalityWithStudents
{
    public record CreateRegisterModalityWithStudentsCommand(RegisterModalityWithStudentsCreateDto Dto) : IRequest<RegisterModalityWithStudentsDto>;
}