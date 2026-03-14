using Domain.Common.Notifications;
using FluentValidation;

namespace Application.Validations.Common
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
}
