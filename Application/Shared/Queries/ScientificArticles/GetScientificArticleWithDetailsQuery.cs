using Application.Shared.DTOs.ScientificArticles;
using MediatR;

namespace Application.Shared.Queries.ScientificArticles
{
    public record GetScientificArticleWithDetailsQuery(int Id) : IRequest<ScientificArticleWithDetailsDto>;
}
