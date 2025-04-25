namespace Domain.Interfaces.Registration
{
    /// <summary>
    /// Interfaz marcadora para identificar servicios que deben registrarse como Transient
    /// </summary>
    public interface ITransientService { }

    /// <summary>
    /// Interfaz marcadora para identificar servicios que deben registrarse como Scoped
    /// </summary>
    public interface IScopedService { }

    /// <summary>
    /// Interfaz marcadora para identificar servicios que deben registrarse como Singleton
    /// </summary>
    public interface ISingletonService { }
}