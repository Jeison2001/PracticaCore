using Application.Shared.DTOs.Document;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Validations.SpecificValidators.Document
{
    public class DocumentUploadDtoValidator : AbstractValidator<DocumentUploadDto>
    {
        public DocumentUploadDtoValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("El archivo es obligatorio.")
                .Must(BeAValidExtension).WithMessage("Formato de archivo no permitido. Solo se aceptan: .pdf, .doc, .docx")
                .Must(BeAValidSize).WithMessage("El archivo excede el tamaño máximo permitido (10MB).");

            RuleFor(x => x.IdDocumentType)
                .GreaterThan(0).When(x => string.IsNullOrEmpty(x.CodeDocumentType))
                .WithMessage("Debe especificar el tipo de documento.");
        }

        private bool BeAValidExtension(IFormFile file)
        {
            if (file == null) return true;
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        private bool BeAValidSize(IFormFile file)
        {
            if (file == null) return true;
            return file.Length <= 10 * 1024 * 1024; // 10MB limit example
        }
    }
}
