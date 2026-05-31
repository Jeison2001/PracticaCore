namespace Domain.Exceptions
{
    /// <summary>
    /// Se lanza cuando un usuario AUTENTICADO intenta una operacion sobre un recurso
    /// al que no tiene acceso (autorizacion a nivel de dato). A diferencia de
    /// <see cref="UnauthorizedAccessException"/> (que .NET asocia a 401 "no autenticado"),
    /// esta excepcion representa el caso 403 "autenticado pero sin permiso" y el
    /// middleware global la traduce a HTTP 403 Forbidden.
    /// </summary>
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message) : base(message) { }
    }
}
