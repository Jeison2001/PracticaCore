using Application.Shared.DTOs.Documents;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Validations.SpecificValidators.Documents
{
    public class DocumentUpdateDtoValidator : AbstractValidator<DocumentUpdateDto>
    {
        public DocumentUpdateDtoValidator()
        {
            RuleFor(x => x.File)
                .Must(BeAValidExtension).WithMessage("Formato de archivo no permitido. Solo se aceptan: .pdf, .doc, .docx")
                .Must(BeAValidSize).WithMessage("El archivo excede el tamaño máximo permitido (10MB).")
                .When(x => x.File != null);
        }

        private bool BeAValidExtension(IFormFile? file)
        {
            if (file == null) return true;
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        private bool BeAValidSize(IFormFile? file)
        {
            if (file == null) return true;
            return file.Length <= 10 * 1024 * 1024; // 10MB limit example
        }
    }
}
