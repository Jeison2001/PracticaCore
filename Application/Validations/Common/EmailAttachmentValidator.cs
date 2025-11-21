using Domain.Common.Notifications;
using FluentValidation;

namespace Application.Validations.Common
{
    /// <summary>
    /// Validador para archivos adjuntos de email
    /// </summary>
    public class EmailAttachmentValidator : AbstractValidator<EmailAttachment>
    {
        public EmailAttachmentValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty()
                .WithMessage("El nombre del archivo es requerido");

            RuleFor(x => x.Content)
                .NotNull()
                .WithMessage("El contenido del archivo es requerido")
                .Must(x => x.Length > 0)
                .WithMessage("El archivo no puede estar vacío")
                .Must(x => x.Length <= 25 * 1024 * 1024) // 25MB máximo
                .WithMessage("El archivo no puede exceder 25MB");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .WithMessage("El tipo de contenido es requerido");
        }
    }
}
