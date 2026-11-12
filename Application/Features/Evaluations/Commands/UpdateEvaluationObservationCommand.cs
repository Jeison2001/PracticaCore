using Application.Shared.DTOs.Evaluations;
using Domain.Common.Users;
using MediatR;

namespace Application.Features.Evaluations.Commands
{
    /// <summary>
    /// Permite al propio autor corregir el texto de una observación/retroalimentación
    /// mientras la etapa o modalidad correspondiente no esté en estado final o rechazado.
    /// </summary>
    public record UpdateEvaluationObservationCommand(
        int Id,
        UpdateEvaluationObservationDto Dto,
        CurrentUserInfo CurrentUser
    ) : IRequest<ObservationHistoryItemDto>;
}
