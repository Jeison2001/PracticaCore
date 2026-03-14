using Application.Shared.DTOs.CoTerminals;
using FluentValidation;

namespace Application.Validations.SpecificValidators.MinorModalities
{
    public class CoTerminalValidator : AbstractValidator<CoTerminalDto>
    {
        public CoTerminalValidator()
        {
            RuleFor(x => x.IdStateStage).NotEmpty();
            RuleFor(x => x.PostgraduateProgramName).MaximumLength(255);
            RuleFor(x => x.UniversityName).MaximumLength(255);
            RuleFor(x => x.FirstSemesterAverage).InclusiveBetween(0, 5).When(x => x.FirstSemesterAverage.HasValue);
        }
    }
}
