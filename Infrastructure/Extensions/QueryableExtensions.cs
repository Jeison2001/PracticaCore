using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
            // PageSize <= 0: sin paginación (el consumidor pide explícitamente todo el dataset)
            if (pageSize <= 0)
                return query;

            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Método que aplica filtrado, ordenamiento y paginación y retorna un resultado paginado.
        /// Modo keyset opcional: con cursor (CursorCreatedAt, CursorId) la paginación es por
        /// cursor (CreatedAt DESC, Id DESC) con coste constante en cualquier profundidad;
        /// el SortBy recibido se ignora y SkipTotalCount omite el COUNT (TotalRecords = -1).
        /// Sin cursor el comportamiento es el clásico (OFFSET + COUNT), retrocompatible.
        /// </summary>
        public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T, TId>(
            this IQueryable<T> query,
            Dictionary<string, string> filters,
            string sortBy,
            bool isDescending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false,
            Func<IQueryable<T>, IQueryable<T>>? include = null)
            where T : BaseEntity<TId>
            where TId : struct
        {
            // Aplicar filtros
            var filteredQuery = query.ApplyFilters<T, TId>(filters);

            // Modo keyset (cursor (CreatedAt, Id)): coste constante en cualquier profundidad.
            // El orden queda forzado a (CreatedAt DESC, Id DESC) para que el cursor sea estable.
            var usingKeyset = cursorId.HasValue && cursorCreatedAt.HasValue && pageSize > 0;
            IQueryable<T> orderedQuery;
            if (usingKeyset)
            {
                var anchorCreatedAt = cursorCreatedAt!.Value;
                // Comparación de tupla (CreatedAt, Id) < (ancla): Id es TId (genérico),
                // así que el predicado se construye con el tipo real. Un Where con las dos
                // condiciones por separado (CreatedAt < a AND Id < c) excluiría filas con
                // CreatedAt anterior al ancla e Id mayor — filas que SÍ siguen en el orden.
                var xParam = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                var createdMember = System.Linq.Expressions.Expression.Property(xParam, nameof(BaseEntity<TId>.CreatedAt));
                var idMember = System.Linq.Expressions.Expression.Property(xParam, nameof(BaseEntity<TId>.Id));
                var anchorConstant = System.Linq.Expressions.Expression.Constant(anchorCreatedAt, typeof(DateTimeOffset));
                var cursorConstant = System.Linq.Expressions.Expression.Constant(
                    Convert.ChangeType(cursorId!.Value, typeof(TId)));
                var ltCreated = System.Linq.Expressions.Expression.LessThan(createdMember, anchorConstant);
                var eqCreated = System.Linq.Expressions.Expression.Equal(createdMember, anchorConstant);
                var ltId = System.Linq.Expressions.Expression.LessThan(
                    idMember, System.Linq.Expressions.Expression.Convert(cursorConstant, typeof(TId)));
                var rowPredicate = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                    System.Linq.Expressions.Expression.OrElse(ltCreated, System.Linq.Expressions.Expression.AndAlso(eqCreated, ltId)),
                    xParam);
                orderedQuery = filteredQuery
                    .Where(rowPredicate)
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id);
            }
            else
            {
                // Aplicar ordenamiento
                orderedQuery = filteredQuery.ApplyOrder(sortBy, isDescending);
                // Desempate estable por Id: sin él, filas con el mismo valor de orden
                // hacen las páginas no deterministas (duplicados/saltos entre páginas).
                if (orderedQuery is System.Linq.IOrderedQueryable<T> orderedId)
                    orderedQuery = orderedId.ThenByDescending(x => Microsoft.EntityFrameworkCore.EF.Property<object>(x, "Id"));
            }

            // Contar total antes de paginar (en keyset con SkipTotalCount se omite:
            // el COUNT con filtro escala igual de mal que el OFFSET).
            // filteredQuery no contiene los includes costosos si se suministra el delegado include.
            var totalCount = usingKeyset && skipTotalCount
                ? -1
                : await filteredQuery.CountAsync(cancellationToken);

            // Aplicar paginación (keyset trae pageSize+1 filas para detectar HasMoreRows sin COUNT)
            var pagedQuery = usingKeyset
                ? orderedQuery.Take(pageSize + 1)
                : orderedQuery.ApplyPaging(pageNumber, pageSize);

            if (include != null)
            {
                pagedQuery = include(pagedQuery);
            }

            // Obtener resultados paginados
            var items = await pagedQuery.ToListAsync(cancellationToken);

            bool? hasMoreRows = null;
            if (usingKeyset)
            {
                hasMoreRows = items.Count > pageSize;
                if (hasMoreRows.Value)
                    items = items.Take(pageSize).ToList();
            }

            // Construir resultado. PageSize <= 0: sin paginación, todo se entrega
            // en una sola página (PageSize = total entregado) para que TotalPages = 1.
            var deliveredPageSize = pageSize > 0 ? pageSize : Math.Max(items.Count, 1);
            return new PaginatedResult<T>
            {
                Items = items,
                TotalRecords = totalCount,
                PageNumber = pageSize > 0 ? pageNumber : 1,
                PageSize = deliveredPageSize,
                HasMoreRows = hasMoreRows
            };
        }
    }
}