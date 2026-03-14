using Application.Shared.DTOs.AcademicAverages;
using FluentValidation;

namespace Application.Validations.SpecificValidators.MinorModalities
{
    public class AcademicAverageValidator : AbstractValidator<AcademicAverageDto>
    {
        public AcademicAverageValidator()
        {
            RuleFor(x => x.IdStateStage).NotEmpty();
            RuleFor(x => x.CertifiedAverage).InclusiveBetween(0, 5).When(x => x.CertifiedAverage.HasValue);
        }
    }
}
