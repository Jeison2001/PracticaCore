using Application.Shared.DTOs.RegisterModalityWithStudents;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Queries.RegisterModalityWithStudents
{
    public record GetRegisterModalityWithStudentsByUserQuery(int IdUser) : IRequest<List<RegisterModalityWithStudentsResponseDto>>;
}