using Application.Shared.DTOs.UserInscriptionModality;
using FluentValidation;

namespace Application.Validations.SpecificValidators.UserInscriptionModality
{
    public class UserInscriptionModalityValidator : AbstractValidator<UserInscriptionModalityDto>
    {
        public UserInscriptionModalityValidator()
        {
            RuleFor(x => x.IdInscriptionModality)
                .NotEmpty().WithMessage("El registro de modalidad es requerido.");

            RuleFor(x => x.IdUser)
                .NotEmpty().WithMessage("El usuario es requerido.");
        }
    }
}