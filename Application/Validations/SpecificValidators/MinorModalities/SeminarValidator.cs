using Application.Shared.DTOs.Seminars;
using FluentValidation;

namespace Application.Validations.SpecificValidators.MinorModalities
{
    public class SeminarValidator : AbstractValidator<SeminarDto>
    {
        public SeminarValidator()
        {
            RuleFor(x => x.IdStateStage).NotEmpty();
            RuleFor(x => x.SeminarName).MaximumLength(255);
            RuleFor(x => x.AttendancePercentage).InclusiveBetween(0, 100).When(x => x.AttendancePercentage.HasValue);
            RuleFor(x => x.FinalGrade).InclusiveBetween(0, 5).When(x => x.FinalGrade.HasValue);
        }
    }
}
