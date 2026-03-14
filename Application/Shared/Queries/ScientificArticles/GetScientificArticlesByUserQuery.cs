using Application.Shared.DTOs.ScientificArticles;
using MediatR;

namespace Application.Shared.Queries.ScientificArticles
{
    public record GetScientificArticlesByUserQuery : IRequest<List<ScientificArticleWithDetailsDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetScientificArticlesByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
