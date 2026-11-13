using Domain.Common.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization
{
    /// <summary>
    /// Evalúa <see cref="PermissionRequirement"/> comprobando intersección con los permisos del usuario autenticado.
    /// </summary>
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userPermissions = PermissionClaimHelper.ExtractPermissionCodes(context.User);

            if (requirement.RequiredPermissions.Any(userPermissions.Contains))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
