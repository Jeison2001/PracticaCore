using FluentValidation;
using Application.Shared.DTOs;

namespace Application.Validations.SpecificValidators.DocumentType
{
    public class DocumentTypeValidator : AbstractValidator<DocumentTypeDto>
    {
        public DocumentTypeValidator()
        {            RuleFor(x => x.IdDocumentClass)
                .GreaterThan(0).WithMessage("La clase de documento es requerida.");
                
            RuleFor(x => x.IdStageModality)
                .GreaterThan(0).WithMessage("La modalidad de etapa debe ser válida.")
                .When(x => x.IdStageModality.HasValue);
                
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código del tipo de documento es requerido.")
                .MaximumLength(100).WithMessage("El código no puede tener más de 100 caracteres.");
                
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del tipo de documento es requerido.")
                .MaximumLength(255).WithMessage("El nombre no puede tener más de 255 caracteres.");
                
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("La descripción no puede tener más de 1000 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
