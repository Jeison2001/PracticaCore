using Application.Shared.DTOs.TeacherEnabledCargos;
using MediatR;

namespace Application.Shared.Queries.TeacherEnabledCargos
{
    public record GetTeacherEnabledCargosQuery : IRequest<List<TeacherEnabledCargoDto>>
    {
        public int UserId { get; init; }
    }
}
