using Application.Shared.DTOs.RegisterModalityStudent;
using FluentValidation;

namespace Application.Validations.specificsValidators.RegisterModalityStudent
{
    public class RegisterModalityStudentValidator : AbstractValidator<RegisterModalityStudentDto>
    {
        public RegisterModalityStudentValidator()
        {
            RuleFor(x => x.IdRegisterModality)
                .NotEmpty().WithMessage("El registro de modalidad es requerido.");

            RuleFor(x => x.IdUser)
                .NotEmpty().WithMessage("El usuario es requerido.");
        }
    }
}