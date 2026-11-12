using Application.Features.Evaluations.Commands;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Evaluations
{
    public class UpdateEvaluationObservationCommandValidator : AbstractValidator<UpdateEvaluationObservationCommand>
    {
        public UpdateEvaluationObservationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El ID de la evaluación debe ser mayor a 0.");

            RuleFor(x => x.Dto)
                .NotNull().WithMessage("El cuerpo de la solicitud es requerido.");

            When(x => x.Dto != null, () =>
            {
                RuleFor(x => x.Dto.Observations)
                    .NotEmpty().WithMessage("La observación no puede estar vacía.")
                    .MaximumLength(4000).WithMessage("La observación no puede superar los 4000 caracteres.");
            });
        }
    }
}
