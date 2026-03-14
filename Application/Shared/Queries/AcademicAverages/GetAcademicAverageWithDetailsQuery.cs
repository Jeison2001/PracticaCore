using Application.Shared.DTOs.AcademicAverages;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages
{
    public record GetAcademicAverageWithDetailsQuery(int Id) : IRequest<AcademicAverageWithDetailsDto>;
}
