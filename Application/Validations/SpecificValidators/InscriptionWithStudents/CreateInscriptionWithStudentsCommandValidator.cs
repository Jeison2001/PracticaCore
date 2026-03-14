using Application.Shared.Commands.InscriptionWithStudents;
using FluentValidation;

namespace Application.Validations.SpecificValidators.InscriptionWithStudents
{
    public class CreateInscriptionWithStudentsCommandValidator : AbstractValidator<CreateInscriptionWithStudentsCommand>
    {
        public CreateInscriptionWithStudentsCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new InscriptionWithStudentsCreateDtoValidator());
        }
    }
}
