using Application.Shared.DTOs.AcademicAverages;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages
{
    public record GetAcademicAveragesByUserQuery : IRequest<List<AcademicAverageWithDetailsDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetAcademicAveragesByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
