using Application.Shared.DTOs;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Queries
{
    public class GetDocumentsByInscriptionModalityQueryHandler : IRequestHandler<GetDocumentsByInscriptionModalityQuery, PaginatedResult<DocumentDto>>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IMapper _mapper;

        public GetDocumentsByInscriptionModalityQueryHandler(IRepository<Document, int> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<DocumentDto>> Handle(GetDocumentsByInscriptionModalityQuery request, CancellationToken cancellationToken)
        {
            // Crear el filtro base por IdInscriptionModality
            Expression<Func<Document, bool>> baseFilter = d => d.IdInscriptionModality == request.IdInscriptionModality;

            // Aplicar filtros adicionales si existen
            var combinedFilters = request.Filters != null 
                ? new Dictionary<string, string>(request.Filters) 
                : new Dictionary<string, string>();

            // Configurar el orden si se especifica
            Func<IQueryable<Document>, IOrderedQueryable<Document>>? orderBy = null;
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                orderBy = query => request.IsDescending
                    ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                    : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
            }

            // Obtener los resultados paginados usando el repositorio
            var paginatedDocs = await _repository.GetAllWithPaginationAsync(
                filter: baseFilter,
                orderBy: orderBy,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize
            );

            // Mapear los resultados al DTO
            var dtoResult = new PaginatedResult<DocumentDto>
            {
                Items = _mapper.Map<IEnumerable<DocumentDto>>(paginatedDocs.Items),
                TotalRecords = paginatedDocs.TotalRecords,
                PageNumber = paginatedDocs.PageNumber,
                PageSize = paginatedDocs.PageSize
            };

            return dtoResult;
        }
    }
}
