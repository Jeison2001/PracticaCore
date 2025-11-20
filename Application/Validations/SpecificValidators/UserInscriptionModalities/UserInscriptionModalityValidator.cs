using Application.Shared.DTOs.UserInscriptionModalities;
using FluentValidation;

namespace Application.Validations.SpecificValidators.UserInscriptionModalities
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