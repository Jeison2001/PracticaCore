using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Domain.Common;
using Domain.Interfaces.Repositories;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Base repository implementation using Entity Framework Core.
    /// Provides standard CRUD operations and supports partial updates via expression-based property tracking.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TId">Primary key type.</typeparam>
    public class BaseRepository<T, TId> : IRepository<T, TId> where T : BaseEntity<TId> where TId : struct
    {
        protected readonly AppDbContext _context;

        public BaseRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Exposes the DbContext for advanced queries that require Include or other EF Core operations.
        /// Use sparingly - prefer standard repository methods when possible.
        /// </summary>
        public AppDbContext Context => _context;

        public async Task<T?> GetByIdAsync(TId id)
            => await _context.Set<T>().FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _context.Set<T>().ToListAsync();

        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = orderBy(query);

            if (pageNumber.HasValue && pageSize.HasValue && pageSize.Value > 0)
                query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);

            return await query.ToListAsync();
        }

        public async Task<PaginatedResult<T>> GetAllWithPaginationAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false)
        {
            IQueryable<T> query = _context.Set<T>().AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            var usingKeyset = (cursorId.HasValue || cursorCreatedAt.HasValue) && pageSize > 0;
            if (usingKeyset)
            {
                var xParam = Expression.Parameter(typeof(T), "x");
                var createdMember = Expression.Property(xParam, nameof(BaseEntity<TId>.CreatedAt));
                var idMember = Expression.Property(xParam, nameof(BaseEntity<TId>.Id));

                if (cursorCreatedAt.HasValue && cursorId.HasValue)
                {
                    var anchorCreatedAt = cursorCreatedAt.Value;
                    var anchorConstant = Expression.Constant(anchorCreatedAt, typeof(DateTimeOffset));
                    var cursorConstant = Expression.Constant(Convert.ChangeType(cursorId.Value, typeof(TId)));

                    var ltCreated = Expression.LessThan(createdMember, anchorConstant);
                    var eqCreated = Expression.Equal(createdMember, anchorConstant);
                    var ltId = Expression.LessThan(idMember, Expression.Convert(cursorConstant, typeof(TId)));

                    var rowPredicate = Expression.Lambda<Func<T, bool>>(
                        Expression.OrElse(ltCreated, Expression.AndAlso(eqCreated, ltId)),
                        xParam);

                    query = query.Where(rowPredicate);
                }
                else if (cursorCreatedAt.HasValue)
                {
                    var anchorCreatedAt = cursorCreatedAt.Value;
                    var anchorConstant = Expression.Constant(anchorCreatedAt, typeof(DateTimeOffset));
                    var ltCreated = Expression.LessThan(createdMember, anchorConstant);
                    var rowPredicate = Expression.Lambda<Func<T, bool>>(ltCreated, xParam);
                    query = query.Where(rowPredicate);
                }
                else if (cursorId.HasValue)
                {
                    var cursorConstant = Expression.Constant(Convert.ChangeType(cursorId.Value, typeof(TId)));
                    var ltId = Expression.LessThan(idMember, Expression.Convert(cursorConstant, typeof(TId)));
                    var rowPredicate = Expression.Lambda<Func<T, bool>>(ltId, xParam);
                    query = query.Where(rowPredicate);
                }

                query = query.OrderByDescending(x => x.CreatedAt)
                             .ThenByDescending(x => x.Id);
            }
            else
            {
                if (orderBy != null)
                {
                    query = orderBy(query);
                }
                else
                {
                    query = query.OrderByDescending(x => x.CreatedAt)
                                 .ThenByDescending(x => x.Id);
                }
            }

            var totalRecords = (usingKeyset && skipTotalCount)
                ? -1
                : (skipTotalCount ? -1 : await query.CountAsync(cancellationToken));

            // PageSize <= 0: sin paginación (entrega todo el resultado en una sola página)
            if (pageSize <= 0)
            {
                pageNumber = 1;
                pageSize = Math.Max(totalRecords, 1);
            }

            var pagedQuery = (usingKeyset || skipTotalCount)
                ? (usingKeyset ? query.Take(pageSize + 1) : query.Skip((pageNumber - 1) * pageSize).Take(pageSize + 1))
                : query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var items = await pagedQuery.ToListAsync(cancellationToken);

            bool? hasMoreRows = null;
            if (usingKeyset || skipTotalCount)
            {
                hasMoreRows = items.Count > pageSize;
                if (hasMoreRows.Value)
                    items = items.Take(pageSize).ToList();
            }

            var lastItem = items.LastOrDefault();
            DateTimeOffset? resCursorCreatedAt = lastItem?.CreatedAt;
            long? resCursorId = lastItem != null ? Convert.ToInt64(lastItem.Id) : null;

            var deliveredPageSize = pageSize > 0 ? pageSize : Math.Max(items.Count, 1);

            return new PaginatedResult<T>
            {
                Items = items,
                TotalRecords = totalRecords,
                PageNumber = pageSize > 0 ? pageNumber : 1,
                PageSize = deliveredPageSize,
                HasMoreRows = hasMoreRows,
                CursorCreatedAt = resCursorCreatedAt,
                CursorId = resCursorId
            };
        }

        public async Task AddAsync(T entity)
            => await _context.Set<T>().AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities)
            => await _context.Set<T>().AddRangeAsync(entities);

        public Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task UpdatePartialAsync(T entity, Expression<Func<T, object?>>[] updatedProperties)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Unchanged)
                entry.State = EntityState.Modified;

            foreach (var property in updatedProperties)
                entry.Property(property).IsModified = true;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
            => await _context.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) return await _context.Set<T>().CountAsync(cancellationToken);
            return await _context.Set<T>().CountAsync(predicate, cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null) return await _context.Set<T>().AnyAsync(cancellationToken);
            return await _context.Set<T>().AnyAsync(predicate, cancellationToken);
        }
    }
}
