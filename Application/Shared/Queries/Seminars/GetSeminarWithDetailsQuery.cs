using Application.Shared.DTOs.Seminars;
using MediatR;

namespace Application.Shared.Queries.Seminars
{
    public record GetSeminarWithDetailsQuery(int Id) : IRequest<SeminarWithDetailsDto>;
}
