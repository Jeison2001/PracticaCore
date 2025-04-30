using System;

namespace Api.SwaggerFilters
{
    /// <summary>
    /// Atributo para marcar propiedades que no deben mostrarse en solicitudes POST de Swagger.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SwaggerExcludeFromPostAttribute : Attribute
    {
    }
}