using Application.Shared.DTOs.SaberPros;
using FluentValidation;

namespace Application.Validations.SpecificValidators.MinorModalities
{
    public class SaberProValidator : AbstractValidator<SaberProDto>
    {
        public SaberProValidator()
        {
            RuleFor(x => x.IdStateStage).NotEmpty();
            RuleFor(x => x.ResultScore).GreaterThanOrEqualTo(0).When(x => x.ResultScore.HasValue);
            RuleFor(x => x.ResultQuintile).MaximumLength(50);
        }
    }
}
