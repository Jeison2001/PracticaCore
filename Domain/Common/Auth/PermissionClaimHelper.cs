using System.Security.Claims;
using System.Text.Json;

namespace Domain.Common.Auth
{
    /// <summary>
    /// Extrae códigos de permiso desde los claims JWT de un <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class PermissionClaimHelper
    {
        /// <summary>
        /// Recorre los claims "permissions" y "permission" (objetos jerárquicos, formato híbrido o arreglos JSON)
        /// y devuelve el conjunto único de códigos de permiso presentes.
        /// </summary>
        public static HashSet<string> ExtractPermissionCodes(ClaimsPrincipal? user)
        {
            var codes = new HashSet<string>(StringComparer.Ordinal);
            if (user == null)
            {
                return codes;
            }

            foreach (var claim in user.Claims)
            {
                if (!string.Equals(claim.Type, "permissions", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(claim.Value))
                {
                    continue;
                }

                try
                {
                    using var document = JsonDocument.Parse(claim.Value);
                    CollectCodes(document.RootElement, codes);
                }
                catch (JsonException)
                {
                    // Ignora claims cuyo contenido no sea JSON válido
                }
            }

            return codes;
        }

        private static void CollectCodes(JsonElement element, HashSet<string> codes)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (!string.IsNullOrWhiteSpace(property.Name))
                        {
                            codes.Add(property.Name.Trim());
                        }
                        CollectCodes(property.Value, codes);
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                codes.Add(value.Trim());
                            }
                        }
                        else if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            CollectCodes(item, codes);
                        }
                    }
                    break;

                case JsonValueKind.String:
                    var scalar = element.GetString();
                    if (!string.IsNullOrWhiteSpace(scalar))
                    {
                        codes.Add(scalar.Trim());
                    }
                    break;
            }
        }
    }
}
