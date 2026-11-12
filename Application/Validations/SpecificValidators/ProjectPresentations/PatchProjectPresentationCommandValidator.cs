using Application.Shared.Commands.ProjectPresentations;
using FluentValidation;

namespace Application.Validations.SpecificValidators.ProjectPresentations
{
    public class PatchProjectPresentationCommandValidator : AbstractValidator<PatchProjectPresentationCommand>
    {
        private static readonly HashSet<string> AllowedModalities = new(StringComparer.OrdinalIgnoreCase)
        {
            "PRESENCIAL",
            "VIRTUAL",
            "MIXTA"
        };

        public PatchProjectPresentationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("El ID de la sustentación debe ser mayor a 0.");

            RuleFor(x => x.Dto)
                .NotNull()
                .WithMessage("Los datos de actualización son obligatorios.");

            When(x => x.Dto != null, () =>
            {
                When(x => x.Dto.IdStateStage.HasValue, () =>
                {
                    RuleFor(x => x.Dto.IdStateStage!.Value)
                        .GreaterThan(0)
                        .WithMessage("El estado de etapa debe ser mayor a 0.");
                });

                When(x => !string.IsNullOrWhiteSpace(x.Dto.PresentationModality), () =>
                {
                    RuleFor(x => x.Dto.PresentationModality!)
                        .Must(m => AllowedModalities.Contains(m.Trim()))
                        .WithMessage("La modalidad de sustentación debe ser PRESENCIAL, VIRTUAL o MIXTA.");
                });

                When(x => x.Dto.LocationOrLink != null, () =>
                {
                    RuleFor(x => x.Dto.LocationOrLink!)
                        .MaximumLength(500)
                        .WithMessage("El lugar o enlace no puede exceder los 500 caracteres.");
                });
            });
        }
    }
}
