using Application.Shared.Commands.Documents;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Documents
{
    public class UpdateDocumentStatusCommandValidator : AbstractValidator<UpdateDocumentStatusCommand>
    {
        public UpdateDocumentStatusCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El ID del documento debe ser mayor a 0.");
        }
    }
}
