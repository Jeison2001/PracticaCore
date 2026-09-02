using Application.Shared.DTOs.TeacherEnabledCargos;
using MediatR;

namespace Application.Shared.Queries.TeacherEnabledCargos
{
    public record GetTeachersByCargoQuery : IRequest<List<TeacherByCargoDto>>
    {
        public int CargoId { get; init; }
    }
}
