using Application.Shared.DTOs;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace Application.Shared.Queries
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
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null;
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                orderBy = query => request.IsDescending
                    ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                    : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
            }

            // Convertir filtros de diccionario a expresión de LINQ
            Expression<Func<T, bool>>? filter = FilterBuilder.BuildFilter<T, TId>(request.Filters);

            var paginatedResult = await _repository.GetAllWithPaginationAsync(
                filter: filter,
                orderBy: orderBy,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize
            );

            var dtoResult = new PaginatedResult<TDto>
            {
                Items = _mapper.Map<IEnumerable<TDto>>(paginatedResult.Items),
                TotalRecords = paginatedResult.TotalRecords,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize
            };

            return dtoResult;
        }
    }
}
