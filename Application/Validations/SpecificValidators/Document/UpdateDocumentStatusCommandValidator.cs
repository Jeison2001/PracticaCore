using Application.Shared.Commands;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Document
{
    public class UpdateDocumentStatusCommandValidator : AbstractValidator<UpdateDocumentStatusCommand>
    {
        public UpdateDocumentStatusCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El ID del documento debe ser mayor a 0.");

            RuleFor(x => x.IdUserUpdateAt)
                .NotNull().WithMessage("El ID del usuario que actualiza es requerido.")
                .GreaterThan(0).WithMessage("El ID del usuario que actualiza debe ser mayor a 0.");

            RuleFor(x => x.OperationRegister)
                .NotEmpty().WithMessage("La operación de registro es requerida.")
                .MaximumLength(100).WithMessage("La operación no puede exceder los 100 caracteres.");
        }
    }
}
