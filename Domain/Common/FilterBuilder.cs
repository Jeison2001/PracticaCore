using System.Linq.Expressions;
using System.Reflection;
using Domain.Entities;

namespace Domain.Common
{
    public static class FilterBuilder
    {
        // Operadores soportados en el formato: propiedad@operador=valor
        private static readonly Dictionary<string, Func<Expression, Expression, Expression>> Operators = new()
        {
            ["eq"] = Expression.Equal,                  // Igual a
            ["ne"] = Expression.NotEqual,               // No igual a
            ["gt"] = Expression.GreaterThan,            // Mayor que
            ["ge"] = Expression.GreaterThanOrEqual,     // Mayor o igual que
            ["lt"] = Expression.LessThan,               // Menor que
            ["le"] = Expression.LessThanOrEqual,        // Menor o igual que
            // Operadores de texto insensibles a mayúsculas/minúsculas y tildes/diacríticos:
            // Se envuelve la columna con EF.Functions.Unaccent(...) de PostgreSQL y ToLower,
            // y se normaliza el valor entrante removiendo diacríticos. De esta forma 'José' coincide con 'jose'
            // y 'gestión' coincide con 'gestion'.
            ["like"] = (property, value) =>             // Contiene
            {
                MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                Expression unaccentProperty = BuildUnaccentExpression(property);
                Expression unaccentValue = BuildUnaccentConstantExpression(value);
                return Expression.Call(
                    Expression.Call(unaccentProperty, toLowerMethod),
                    containsMethod,
                    Expression.Call(unaccentValue, toLowerMethod));
            },
            ["startswith"] = (property, value) =>       // Comienza con
            {
                MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                MethodInfo method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!;
                Expression unaccentProperty = BuildUnaccentExpression(property);
                Expression unaccentValue = BuildUnaccentConstantExpression(value);
                return Expression.Call(
                    Expression.Call(unaccentProperty, toLowerMethod),
                    method,
                    Expression.Call(unaccentValue, toLowerMethod));
            },
            ["endswith"] = (property, value) =>         // Termina con
            {
                MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                MethodInfo method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!;
                Expression unaccentProperty = BuildUnaccentExpression(property);
                Expression unaccentValue = BuildUnaccentConstantExpression(value);
                return Expression.Call(
                    Expression.Call(unaccentProperty, toLowerMethod),
                    method,
                    Expression.Call(unaccentValue, toLowerMethod));
            }
        };

        private static Expression BuildUnaccentExpression(Expression property)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type? npgsqlType = null;
                Type? efType = null;
                Type? dbFunctionsType = null;

                foreach (var asm in assemblies)
                {
                    if (npgsqlType == null && asm.GetName().Name?.StartsWith("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        npgsqlType = asm.GetType("Microsoft.EntityFrameworkCore.NpgsqlDbFunctionsExtensions");
                    }
                    if (efType == null && asm.GetName().Name?.Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        efType = asm.GetType("Microsoft.EntityFrameworkCore.EF");
                        dbFunctionsType = asm.GetType("Microsoft.EntityFrameworkCore.DbFunctions");
                    }
                }

                if (efType != null && npgsqlType != null && dbFunctionsType != null)
                {
                    var efFunctionsProp = efType.GetProperty("Functions");
                    var unaccentMethod = npgsqlType.GetMethod("Unaccent", new[] { dbFunctionsType, typeof(string) });
                    if (efFunctionsProp != null && unaccentMethod != null)
                    {
                        var functionsExpr = Expression.Property(null, efFunctionsProp);
                        return Expression.Call(null, unaccentMethod, functionsExpr, property);
                    }
                }
            }
            catch
            {
                // Fallback a property sin unaccent si el provider no está presente (ej. SQLite o memoria pura)
            }
            return property;
        }

        private static Expression BuildUnaccentConstantExpression(Expression value)
        {
            if (value is ConstantExpression constant && constant.Value is string strVal)
            {
                return Expression.Constant(RemoveDiacritics(strVal), typeof(string));
            }
            return value;
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        public static Expression<Func<T, bool>>? BuildFilter<T, TId>(Dictionary<string, string>? filters) 
            where T : BaseEntity<TId> 
            where TId : struct
        {
            if (filters == null || !filters.Any())
                return null;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            Expression? finalExpression = null;

            foreach (var filter in filters)
            {
                var key = filter.Key;
                var value = filter.Value;

                // Extraer el operador si existe (formato: propiedad@operador)
                string propertyName = key;
                string operatorName = "eq"; // Por defecto es igual (equal)

                if (key.Contains('@'))
                {
                    var parts = key.Split('@', 2);
                    propertyName = parts[0];
                    operatorName = parts[1].ToLower();
                }

                // Validar que la propiedad existe en la entidad
                PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName, 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                
                if (propertyInfo == null)
                    continue;

                // Construir la expresión para esta propiedad
                MemberExpression property = Expression.Property(parameter, propertyInfo);
                
                // Convertir el valor al tipo de la propiedad
                object? convertedValue = ConvertValue(value, propertyInfo.PropertyType);
                if (convertedValue == null && operatorName != "like" && operatorName != "startswith" && operatorName != "endswith")
                    continue;

                // Crear la expresión según el operador
                Expression? comparisonExpression = null;
                
                if (Operators.TryGetValue(operatorName, out var operation))
                {
                    ConstantExpression constantValue = Expression.Constant(
                        convertedValue, 
                        // Para operadores de texto, siempre usar string
                        operatorName is "like" or "startswith" or "endswith" 
                            ? typeof(string) 
                            : propertyInfo.PropertyType);
                    
                    comparisonExpression = operation(property, constantValue);
                }
                else
                {
                    // Si no se reconoce el operador, usar igualdad por defecto
                    ConstantExpression constantValue = Expression.Constant(convertedValue, propertyInfo.PropertyType);
                    comparisonExpression = Expression.Equal(property, constantValue);
                }
                
                // Combinar con la expresión final usando AND
                finalExpression = finalExpression == null 
                    ? comparisonExpression 
                    : Expression.AndAlso(finalExpression, comparisonExpression);
            }

            if (finalExpression == null)
                return null;

            // Crear la expresión lambda
            return Expression.Lambda<Func<T, bool>>(finalExpression, parameter);
        }

        /// <summary>
        /// Devuelve las claves cuyo VALOR (u operador) no es válido para la entidad y que
        /// BuildFilter ignoraría en silencio. Es solo diagnóstico: no altera el filtrado.
        /// </summary>
        public static List<string> GetInvalidFilterKeys<T, TId>(Dictionary<string, string>? filters)
            where T : BaseEntity<TId>
            where TId : struct
        {
            var invalid = new List<string>();
            if (filters == null)
                return invalid;

            foreach (var filter in filters)
            {
                var key = filter.Key;
                string propertyName = key;
                string operatorName = "eq";

                if (key.Contains('@'))
                {
                    var parts = key.Split('@', 2);
                    propertyName = parts[0];
                    operatorName = parts[1].ToLower();
                }

                PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                // Clave desconocida: la reporta el handler aparte.
                if (propertyInfo == null)
                    continue;

                // Operador desconocido: BuildFilter caería a igualdad sin avisar.
                if (operatorName is not ("eq" or "ne" or "gt" or "ge" or "lt" or "le" or "like" or "startswith" or "endswith"))
                {
                    invalid.Add(key);
                    continue;
                }

                // Texto libre: cualquier valor es válido.
                if (operatorName is "like" or "startswith" or "endswith")
                    continue;

                if (ConvertValue(filter.Value, propertyInfo.PropertyType) == null)
                    invalid.Add(key);
            }

            return invalid;
        }

        private static object? ConvertValue(string value, Type targetType)
        {
            try
            {
                if (targetType == typeof(Guid))
                    return Guid.Parse(value);
                
                if (targetType == typeof(DateTime))
                    return DateTime.Parse(value);

                // Las fechas del dominio son DateTimeOffset (CreatedAt/UpdatedAt, etc.)
                if (targetType == typeof(DateTimeOffset))
                    return DateTimeOffset.Parse(value);
                
                if (targetType == typeof(bool))
                    return bool.Parse(value);
                
                if (targetType == typeof(int))
                    return int.Parse(value);
                
                if (targetType == typeof(double))
                    return double.Parse(value);
                
                if (targetType == typeof(decimal))
                    return decimal.Parse(value);
                
                // Para tipos nullable
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Type underlyingType = Nullable.GetUnderlyingType(targetType)!;
                    return ConvertValue(value, underlyingType);
                }
                
                return value;
            }
            catch
            {
                // En caso de error de conversión, retornamos null
                return null;
            }
        }
    }
}