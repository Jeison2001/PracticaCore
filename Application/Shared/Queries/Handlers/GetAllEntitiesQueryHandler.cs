using Application.Shared.DTOs;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.Handlers
{
    public class GetAllEntitiesQueryHandler<T, TId, TDto> : IRequestHandler<GetAllEntitiesQuery<T, TId, TDto>, PaginatedResult<TDto>>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;

        public GetAllEntitiesQueryHandler(IRepository<T, TId> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<TDto>> Handle(GetAllEntitiesQuery<T, TId, TDto> request, CancellationToken ct)
        {
            var usingKeyset = (request.CursorCreatedAt.HasValue || request.CursorId.HasValue) && request.PageSize > 0;
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy;

            if (usingKeyset)
            {
                // Keyset: orden forzado a (CreatedAt DESC, Id DESC) para estabilidad del cursor
                orderBy = query => query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id);
            }
            else if (!string.IsNullOrEmpty(request.SortBy))
            {
                orderBy = query =>
                {
                    var ordered = request.IsDescending
                        ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                        : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
                    return ordered.ThenByDescending(e => EF.Property<object>(e, "Id"));
                };
            }
            else
            {
                orderBy = query => query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id);
            }

            // Convertir filtros de diccionario a expresión de LINQ
            Expression<Func<T, bool>>? filter = FilterBuilder.BuildFilter<T, TId>(request.Filters);

            var paginatedResult = await _repository.GetAllWithPaginationAsync(
                filter: filter,
                orderBy: orderBy,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: ct,
                cursorId: request.CursorId,
                cursorCreatedAt: request.CursorCreatedAt,
                skipTotalCount: request.SkipTotalCount
            );

            var dtoResult = new PaginatedResult<TDto>
            {
                Items = _mapper.Map<IEnumerable<TDto>>(paginatedResult.Items),
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                HasMoreRows = paginatedResult.HasMoreRows,
                CursorCreatedAt = paginatedResult.CursorCreatedAt,
                CursorId = paginatedResult.CursorId
            };

            return dtoResult;
        }
    }
}