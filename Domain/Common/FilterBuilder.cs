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
            ["like"] = (property, value) =>             // Contiene (LIKE)
            {
                MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                return Expression.Call(property, containsMethod, value);
            },
            ["startswith"] = (property, value) =>       // Comienza con
            {
                MethodInfo method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!;
                return Expression.Call(property, method, value);
            },
            ["endswith"] = (property, value) =>         // Termina con
            {
                MethodInfo method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!;
                return Expression.Call(property, method, value);
            }
        };

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

        private static object? ConvertValue(string value, Type targetType)
        {
            try
            {
                if (targetType == typeof(Guid))
                    return Guid.Parse(value);
                
                if (targetType == typeof(DateTime))
                    return DateTime.Parse(value);
                
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