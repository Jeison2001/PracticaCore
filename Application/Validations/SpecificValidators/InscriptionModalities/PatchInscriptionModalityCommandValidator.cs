using Application.Shared.Commands.InscriptionModalities;
using FluentValidation;

namespace Application.Validations.SpecificValidators.InscriptionModalities
{
    public class PatchInscriptionModalityCommandValidator : AbstractValidator<PatchInscriptionModalityCommand>
    {
        public PatchInscriptionModalityCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El ID de la inscripción debe ser mayor a 0");
        }
    }
}
