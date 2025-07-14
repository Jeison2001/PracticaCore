using Domain.Common;
using FluentValidation;

namespace Application.Validations.BaseValidators
{
    /// <summary>
    /// Validador para la notificación por email
    /// </summary>
    public class EmailNotificationValidator : AbstractValidator<EmailNotification>
    {
        public EmailNotificationValidator()
        {
            RuleFor(x => x.To)
                .NotEmpty()
                .WithMessage("El destinatario es requerido")
                .EmailAddress()
                .WithMessage("El destinatario debe ser una dirección de correo válida");

            RuleFor(x => x.Subject)
                .NotEmpty()
                .WithMessage("El asunto es requerido")
                .MaximumLength(500)
                .WithMessage("El asunto no puede exceder 500 caracteres");

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithMessage("El cuerpo del mensaje es requerido");

            RuleForEach(x => x.Cc)
                .EmailAddress()
                .WithMessage("Todas las direcciones en CC deben ser válidas");

            RuleForEach(x => x.Bcc)
                .EmailAddress()
                .WithMessage("Todas las direcciones en BCC deben ser válidas");

            RuleFor(x => x.From)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.From))
                .WithMessage("La dirección del remitente debe ser válida");

            RuleForEach(x => x.Attachments)
                .SetValidator(new EmailAttachmentValidator());
        }
    }

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
