using Application.Shared.DTOs.RegisterModalityWithStudents;
using MediatR;

namespace Application.Shared.Commands.RegisterModalityWithStudents
{
    public record UpdateRegisterModalityWithStudentsCommand(int Id, RegisterModalityWithStudentsDto Dto) : IRequest<RegisterModalityWithStudentsDto>;
}