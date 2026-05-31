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
        Task EnsureUserCanModifyAsync(Document entity, int currentUserId, CancellationToken cancellationToken = default);
    }
}
