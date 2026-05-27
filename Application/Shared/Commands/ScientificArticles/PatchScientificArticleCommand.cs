using Application.Shared.DTOs.ScientificArticles;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.ScientificArticles
{
    public record PatchScientificArticleCommand(
        int Id,
        ScientificArticlePatchDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<ScientificArticleDto>;
}
