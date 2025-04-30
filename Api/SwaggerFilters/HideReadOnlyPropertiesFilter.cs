using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.SwaggerFilters
{
    /// <summary>
    /// Filtro de Swagger para ocultar propiedades que no son necesarias en las solicitudes POST y PUT.
    /// </summary>
    public class HideReadOnlyPropertiesFilter : IOperationFilter
    {
        private readonly HashSet<string> _postPropertiesToHide = new(StringComparer.OrdinalIgnoreCase)
        { 
            "Id", "CreatedAt", "UpdatedAt", "IdUserUpdatedAt"
        };

        private readonly HashSet<string> _putPropertiesToHide = new(StringComparer.OrdinalIgnoreCase)
        { 
            "CreatedAt", "IdUserCreatedAt"
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                // No aplicar si no hay método HTTP
                if (string.IsNullOrEmpty(context.ApiDescription.HttpMethod))
                    return;

                // Verificar si hay un esquema de solicitud
                if (operation.RequestBody?.Content == null || operation.RequestBody.Content.Count == 0)
                    return;

                var isPost = context.ApiDescription.HttpMethod == "POST";
                var isPut = context.ApiDescription.HttpMethod == "PUT";
                
                // Si no es POST ni PUT, no aplicamos ningún filtro
                if (!isPost && !isPut)
                    return;

                foreach (var contentType in operation.RequestBody.Content)
                {
                    if (contentType.Value?.Schema?.Reference == null)
                        continue;

                    var schemaReferenceId = contentType.Value.Schema.Reference.Id;
                    if (string.IsNullOrEmpty(schemaReferenceId))
                        continue;

                    if (!context.SchemaRepository.Schemas.TryGetValue(schemaReferenceId, out var schema))
                        continue;

                    if (schema.Properties == null)
                        continue;

                    // Procesamos cada propiedad del esquema
                    foreach (var prop in schema.Properties)
                    {
                        var propertyName = prop.Key;
                        var propertySchema = prop.Value;

                        // Para operaciones POST
                        if (isPost && _postPropertiesToHide.Contains(propertyName))
                        {
                            propertySchema.ReadOnly = true;
                        }
                        // Para operaciones PUT
                        else if (isPut && _putPropertiesToHide.Contains(propertyName))
                        {
                            propertySchema.ReadOnly = true;
                        }
                    }
                }
            }
            catch
            {
                // Captura cualquier excepción para evitar que Swagger deje de funcionar
            }
        }
    }
}