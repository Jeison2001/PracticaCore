using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Extensions
{
    /// <summary>
    /// Métodos de extensión para IQueryable que facilitan la paginación, filtrado y ordenamiento dinámico
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Aplica filtros dinámicos a una consulta IQueryable utilizando FilterBuilder
        /// </summary>
        public static IQueryable<T> ApplyFilters<T, TId>(
            this IQueryable<T> query,
            Dictionary<string, string> filters) 
            where T : BaseEntity<TId> 
            where TId : struct
        {
            var filter = FilterBuilder.BuildFilter<T, TId>(filters);
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return query;
        }

        /// <summary>
        /// Aplica ordenamiento dinámico a una consulta IQueryable
        /// </summary>
        public static IQueryable<T> ApplyOrder<T>(
            this IQueryable<T> query,
            string sortBy,
            bool isDescending)
            where T : class
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                // Si no hay campo de ordenamiento, por defecto se ordena por Id si existe
                var entityType = typeof(T);
                var idProperty = entityType.GetProperty("Id");
                if (idProperty != null)
                {
                    return isDescending
                        ? query.OrderByDynamic("Id", isDescending)
                        : query.OrderByDynamic("Id", isDescending);
                }
                return query; // Si no hay propiedad Id, devuelve la consulta sin ordenar
            }

            return query.OrderByDynamic(sortBy, isDescending);
        }

        /// <summary>
        /// Ordena una consulta IQueryable por una propiedad especificada en tiempo de ejecución
        /// </summary>
        public static IQueryable<T> OrderByDynamic<T>(
            this IQueryable<T> query,
            string propertyName,
            bool isDescending)
            where T : class
        {
            if (string.IsNullOrEmpty(propertyName))
                return query;

            try
            {
                // Si es una propiedad de navegación (contiene un punto)
                if (propertyName.Contains('.'))
                {
                    var parts = propertyName.Split('.');
                    if (parts.Length != 2)
                        return query;

                    var navigation = parts[0];
                    var property = parts[1];

                    // Obtener el tipo de la entidad principal
                    var entityType = typeof(T);
                    
                    // Obtener la propiedad de navegación
                    var navigationProperty = entityType.GetProperty(navigation);
                    if (navigationProperty == null)
                        return query;

                    // Usar EF Core para ordenar por una propiedad de navegación dinámicamente
                    return isDescending
                        ? query.OrderByDescending(e => EF.Property<object>(EF.Property<object>(e, navigation), property))
                        : query.OrderBy(e => EF.Property<object>(EF.Property<object>(e, navigation), property));
                }
                else
                {
                    // Ordenar por una propiedad directa
                    return isDescending
                        ? query.OrderByDescending(e => EF.Property<object>(e, propertyName))
                        : query.OrderBy(e => EF.Property<object>(e, propertyName));
                }
            }
            catch
            {
                // Si hay un error (ej. propiedad no existe), devolver la consulta sin ordenar
                return query;
            }
        }

        /// <summary>
        /// Aplica paginación a una consulta IQueryable
        /// </summary>
        public static IQueryable<T> ApplyPaging<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
            where T : class
        {
            if (pageNumber <= 0)
                pageNumber = 1;
            if (pageSize <= 0)
                pageSize = 10;

            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Método que aplica filtrado, ordenamiento y paginación y retorna un resultado paginado
        /// </summary>
        public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T, TId>(
            this IQueryable<T> query,
            Dictionary<string, string> filters,
            string sortBy,
            bool isDescending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
            where T : BaseEntity<TId>
            where TId : struct
        {
            // Aplicar filtros
            var filteredQuery = query.ApplyFilters<T, TId>(filters);
            
            // Contar total antes de paginar
            var totalCount = await filteredQuery.CountAsync(cancellationToken);
            
            // Aplicar ordenamiento
            var orderedQuery = filteredQuery.ApplyOrder(sortBy, isDescending);
            
            // Aplicar paginación
            var pagedQuery = orderedQuery.ApplyPaging(pageNumber, pageSize);
            
            // Obtener resultados paginados
            var items = await pagedQuery.ToListAsync(cancellationToken);
            
            // Construir resultado
            return new PaginatedResult<T>
            {
                Items = items,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}