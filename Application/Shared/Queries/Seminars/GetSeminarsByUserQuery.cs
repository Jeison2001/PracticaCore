using Application.Shared.DTOs.Seminars;
using MediatR;

namespace Application.Shared.Queries.Seminars
{
    public record GetSeminarsByUserQuery : IRequest<List<SeminarWithDetailsDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetSeminarsByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
