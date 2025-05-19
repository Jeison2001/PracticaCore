using Application.Shared.DTOs;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Document
{
    public class DocumentUpdateDtoValidator : AbstractValidator<DocumentUpdateDto>
    {
        public DocumentUpdateDtoValidator()
        {
            RuleFor(x => x.IdDocumentType)
                .Must((dto, id) => id > 0 || !string.IsNullOrWhiteSpace(dto.CodeDocumentType))
                .WithMessage("Debe especificar IdDocumentType o CodeDocumentType.");

            RuleFor(x => x.CodeDocumentType)
                .Must((dto, code) => string.IsNullOrWhiteSpace(code) || dto.IdDocumentType == 0)
                .WithMessage("No puede especificar ambos: IdDocumentType y CodeDocumentType. Use solo uno.");
        }
    }
}
