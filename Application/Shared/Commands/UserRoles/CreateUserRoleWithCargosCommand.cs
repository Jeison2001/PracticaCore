using Application.Shared.DTOs.UserRoles;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Commands.UserRoles
{
    /// <summary>
    /// Asigna un rol a un usuario y, si el rol es TEACHER y el DTO trae cargos habilitados
    /// (TypeTeachingAssignmentIds), los asigna en el mismo commit — operación atómica.
    /// </summary>
    public record CreateUserRoleWithCargosCommand : IRequest<UserRoleDto>
    {
        public UserRoleDto Dto { get; init; } = null!;
        public CurrentUserInfo CurrentUser { get; init; } = null!;
    }
}
