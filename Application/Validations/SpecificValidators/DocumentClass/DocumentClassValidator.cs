using FluentValidation;
using Application.Shared.DTOs.DocumentClass;

namespace Application.Validations.SpecificValidators.DocumentClass
{
    public class DocumentClassValidator : AbstractValidator<DocumentClassDto>
    {
        public DocumentClassValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código de la clase de documento es requerido.")
                .MaximumLength(100).WithMessage("El código no puede tener más de 100 caracteres.");
                
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de la clase de documento es requerido.")
                .MaximumLength(255).WithMessage("El nombre no puede tener más de 255 caracteres.");
                
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("La descripción no puede tener más de 1000 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
