using Application.Shared.Commands.AcademicPractice;
using FluentValidation;

namespace Application.Validations.SpecificValidators.AcademicPractice
{
    public class UpdateAcademicPracticeInstitutionCommandValidator : AbstractValidator<UpdateAcademicPracticeInstitutionCommand>
    {
        public UpdateAcademicPracticeInstitutionCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new UpdateInstitutionInfoDtoValidator());
        }
    }
}
