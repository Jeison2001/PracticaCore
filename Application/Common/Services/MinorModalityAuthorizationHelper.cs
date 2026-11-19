using Domain.Common.Users;
using Domain.Constants;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services
{
    /// <summary>
    /// Valida autorización de propiedad y anti-auto-aprobación para modalidades menores en operaciones PATCH.
    /// </summary>
    public static class MinorModalityAuthorizationHelper
    {
        public static async Task EnsureAuthorizedForPatchAsync(
            IUnitOfWork unitOfWork,
            int entityId,
            int? newStateStageId,
            CurrentUserInfo? currentUser,
            string[] managementAndReviewerPerms,
            CancellationToken cancellationToken = default)
        {
            if (currentUser == null ||
                currentUser.UserId is not > 0 ||
                currentUser.Permissions is not { Count: > 0 } perms)
            {
                throw new ForbiddenAccessException("No tiene permisos vigentes para realizar esta acción.");
            }

            if (managementAndReviewerPerms.Any(perms.Contains))
            {
                return;
            }

            await ModalityAccessAuthorizationHelper.EnsureStudentAuthorizedForInscriptionAsync(
                unitOfWork,
                entityId,
                newStateStageId,
                currentUser,
                "No está autorizado para modificar una inscripción a la cual no pertenece.",
                "El estudiante no tiene permisos para aprobar o rechazar su propia modalidad.",
                cancellationToken);
        }
    }
}
