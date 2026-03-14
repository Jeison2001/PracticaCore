using Application.Shared.Commands.AcademicPractices;
using FluentValidation;

namespace Application.Validations.SpecificValidators.AcademicPractices
{
    public class UpdateAcademicPracticeInstitutionCommandValidator : AbstractValidator<UpdateAcademicPracticeInstitutionCommand>
    {
        public UpdateAcademicPracticeInstitutionCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new UpdateInstitutionInfoDtoValidator());
        }
    }
}
