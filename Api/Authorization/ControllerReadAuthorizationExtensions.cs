using System.Security.Claims;
using Application.Common.Services;
using Domain.Common.Auth;
using Domain.Common.Extensions;
using Domain.Constants;
using Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Authorization
{
    /// <summary>
    /// Utilidades centralizadas anti-IDOR para endpoints de consulta (/ByUser/{id}, /ByTeacher/{id},
    /// /WithDetails/{id}, /Inscription/{id}). Fallan cerrado cuando el actor autenticado no es el propio
    /// usuario/docente ni pertenece a la inscripción con un permiso de gestión habilitado.
    /// </summary>
    internal static class ControllerReadAuthorizationExtensions
    {
        internal static bool IsCrossActorAccessDenied(
            this ControllerBase controller,
            int targetActorId,
            params string[] overridePermissions)
        {
            var user = controller.User;
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var currentUserId = user.GetUserId();
            if (!currentUserId.HasValue || currentUserId.Value <= 0)
                return true;

            if (currentUserId.Value == targetActorId)
                return false;

            var permissions = PermissionClaimHelper.ExtractPermissionCodes(user);
            if (permissions.Contains(PermissionCodes.Configuraciones.N3CFSU))
                return false;

            return !overridePermissions.Any(permissions.Contains);
        }

        internal static async Task<bool> IsInscriptionReadAccessDeniedAsync(
            this ControllerBase controller,
            int inscriptionId,
            CancellationToken cancellationToken = default,
            params string[] managementPermissions)
        {
            var user = controller.User;
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var currentUser = user.GetCurrentUserInfo();
            if (currentUser.UserId is null or <= 0)
                return true;

            var permissions = currentUser.Permissions;
            if (permissions != null &&
                (permissions.Contains(PermissionCodes.Configuraciones.N3CFSU) ||
                 managementPermissions.Any(permissions.Contains)))
            {
                return false;
            }

            if (inscriptionId <= 0)
                return true;

            var unitOfWork = controller.HttpContext?.RequestServices.GetService<IUnitOfWork>();
            if (unitOfWork == null)
                return true;

            if (await ModalityAccessAuthorizationHelper.IsUserInInscriptionAsync(
                    unitOfWork, inscriptionId, currentUser.UserId.Value, cancellationToken))
            {
                return false;
            }

            if (await ModalityAccessAuthorizationHelper.IsConfirmedTeacherInInscriptionAsync(
                    unitOfWork, inscriptionId, currentUser.UserId.Value, cancellationToken))
            {
                return false;
            }

            return true;
        }
    }
}
