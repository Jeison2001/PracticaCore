namespace Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"Entidad '{name}' con ID '{key}' no encontrada.") { }
    }
}
