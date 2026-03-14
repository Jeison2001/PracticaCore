using Application.Shared.DTOs.ScientificArticles;
using FluentValidation;

namespace Application.Validations.SpecificValidators.MinorModalities
{
    public class ScientificArticleValidator : AbstractValidator<ScientificArticleDto>
    {
        public ScientificArticleValidator()
        {
            RuleFor(x => x.IdStateStage).NotEmpty();
            RuleFor(x => x.ArticleTitle).MaximumLength(255);
            RuleFor(x => x.JournalName).MaximumLength(255);
            RuleFor(x => x.ISSN).MaximumLength(50);
            RuleFor(x => x.JournalCategory).MaximumLength(50);
        }
    }
}
