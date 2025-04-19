using Application.Shared.DTOs.RegisterModalityWithStudents;
using MediatR;

namespace Application.Shared.Queries.RegisterModalityWithStudents
{
    public record GetRegisterModalityWithStudentsQuery(int Id) : IRequest<RegisterModalityWithStudentsDto>;
}