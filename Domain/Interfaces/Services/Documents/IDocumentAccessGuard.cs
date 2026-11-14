using Domain.Common.Users;
using Domain.Entities;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services.Documents
{
    /// <summary>
    /// Guard de autorizacion a nivel de dato (anti-IDOR) para documentos. Verifica que el
    /// usuario autenticado este vinculado a la inscripcion del documento (como estudiante via
    /// <c>UserInscriptionModality</c> o como docente via <c>TeachingAssignment</c>) antes de
    /// permitir modificarlo. Es la unica autorizacion a nivel de dato del backend: el RBAC
    /// N1-N4 solo filtra el frontend, no se aplica aqui.
    /// </summary>
    public interface IDocumentAccessGuard : IScopedService
    {
        /// <summary>
        /// Lanza si el usuario no puede modificar el documento. Fail-closed: si no hay un
        /// usuario identificado (<paramref name="currentUserId"/> &lt;= 0) o el documento no
        /// tiene inscripcion asociada, se deniega.
        /// </summary>
        /// <exception cref="Domain.Exceptions.ForbiddenAccessException">
        /// Cuando el usuario no esta vinculado a la inscripcion del documento.
        /// </exception>
        Task EnsureUserCanModifyAsync(
            Document entity,
            int currentUserId,
            CancellationToken cancellationToken = default,
            IReadOnlyCollection<string>? userPermissions = null);

        /// <summary>
        /// Lanza si el usuario no puede consultar o descargar documentos de la inscripcion.
        /// Fail-closed: si <paramref name="currentUser"/> no tiene identidad valida o
        /// <paramref name="inscriptionModalityId"/> es nulo o &lt;= 0, se deniega.
        /// Permite lectura global solo con permisos de gestion/revision global autorizados,
        /// o a estudiantes activos y docentes activos confirmados vinculados a la inscripcion.
        /// </summary>
        /// <exception cref="Domain.Exceptions.ForbiddenAccessException">
        /// Cuando el usuario no tiene permiso global de lectura ni esta vinculado a la inscripcion.
        /// </exception>
        Task EnsureUserCanReadAsync(
            int? inscriptionModalityId,
            CurrentUserInfo currentUser,
            int? stageModalityId = null,
            int? documentTypeId = null,
            CancellationToken cancellationToken = default);
    }
}
